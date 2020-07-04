using System;
using System.Linq;
using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using ElasticQuery.Exporter.HealthCheck;
using ElasticQuery.Exporter.Lib.Extension;
using ElasticQuery.Exporter.Lib.File;
using ElasticQuery.Exporter.Middleware;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using ElasticQuery.Exporter.Scheduler;
using ElasticQuery.Exporter.Services.MetricsWriter;
using ElasticQuery.Exporter.Services.QueryExecutor;
using ElasticQuery.Exporter.Services.QueryProvider;
using ElasticQuery.Exporter.Validators;
using Elasticsearch.Net;
using FluentValidation;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ElasticQuery.Exporter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ExporterConfiguration = Configuration.GetSection("Exporter");

            if (!ExporterConfiguration.Exists())
            {
                throw new InvalidOperationException("Exporter options not specified");
            }
        }

        public IConfiguration Configuration { get; }

        public IConfigurationSection ExporterConfiguration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMetrics(s =>
            {
                var options = ExporterConfiguration.CreateOptions<ExporterOptions>(o =>
                {
                    var validator = new ExporterOptionsValidator();
                    validator.ValidateAndThrow(o);
                });

                s.Configuration.Configure(c =>
                {
                    c.DefaultContextLabel = options.Metrics.Prefix;
                    
                    if (options.Metrics.Labels != null)
                        c.GlobalTags = new GlobalMetricTags(options.Metrics.Labels);
                });

                s.OutputMetrics.AsPrometheusPlainText();
            });

            services.AddMetricsEndpoints(o =>
            {
                o.MetricsTextEndpointEnabled = false;

                o.MetricsEndpointEnabled = true;
                o.MetricsEndpointOutputFormatter = o.MetricsOutputFormatters
                    .OfType<MetricsPrometheusTextOutputFormatter>().First();
            });

            services.AddHangfire(o =>
            {
                o.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseRecommendedSerializerSettings()
                    .UseMemoryStorage();
            });

            services.AddSingleton<MetricsSchedulerInitializedHealthCheck>();
            services.AddHealthChecks()
                .AddCheck<MetricsSchedulerInitializedHealthCheck>(
                    name: "scheduler",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "ready" })
                .AddTypeActivatedCheck<ElasticSearchAggregateHealthCheck>(
                    name: "elasticsearch",
                    tags: new[] { "live" },
                    failureStatus: HealthStatus.Degraded);

            // Exporter
            services.AddOptions<ExporterOptions>()
                .Configure(o =>
                {
                    ExporterConfiguration.Bind(o);
                })
                .Validate<IValidator<ExporterOptions>, ILogger<ExporterOptions>>((options, validator, logger) =>
                {
                    var result = validator.Validate(options);

                    if (!result.IsValid)
                        logger.LogError("Configuration is invalid: {Reason}", result.ToString());

                    return result.IsValid;
                });


            services.AddSingleton(_ => new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build());

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ExporterOptions>>().Value;
                var pool = options switch
                {
                    _ when options.ElasticSearch.Connection.SingleNode != null
                        => (IConnectionPool) new SingleNodeConnectionPool(new Uri(options.ElasticSearch.Connection.SingleNode.Url)),
                    _ when options.ElasticSearch.Connection.StaticCluster != null
                        => (IConnectionPool) new StaticConnectionPool(options.ElasticSearch.Connection.StaticCluster.Urls
                            .Select(u => new Uri(u))),
                    _ => null
                };

                if (pool == null)
                    throw new Exception("Unable to construct ES connection pool");

                var settings = new ConnectionSettings(pool)
                    .RequestTimeout(options.ElasticSearch.RequestTimeout);

                return new ElasticClient(settings);
            });

            services.AddSingleton<IGlobFileProvider>(sp =>
            {
                var environment = sp.GetRequiredService<IHostEnvironment>();
                return new GlobFileProvider(environment.ContentRootPath);
            });

            services.AddTransient<IValidator<MetricQuery>, MetricQueryValidator>();
            services.AddTransient<IValidator<ExporterOptions>, ExporterOptionsValidator>();

            services.AddSingleton<IMetricQueriesProvider, FileMetricQueriesProvider>();
            services.AddSingleton<IMetricQueryExecutor, MetricQueryExecutor>();
            services.AddSingleton<IMetricsWriter, MetricsWriter>();

            services.AddTransient<ScheduledMetricsEvaluator>();
            services.AddHostedService<ScheduledMetricsInitializer>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                var builder = endpoints
                    .CreateApplicationBuilder()
                    .UseMiddleware<OndemandMetricsEvaluationMiddleware>()
                    .UseMetricsEndpoint();

                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = hc =>  hc.Tags.Contains("ready"),
                    ResponseWriter = HealthCheckJsonWriter.WriteAsync,
                });

                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = hc => hc.Tags.Contains("live"),
                    ResponseWriter = HealthCheckJsonWriter.WriteAsync
                });

                endpoints.MapGet("/metrics", builder.Build());
            });

            app.UseHangfireServer();
            app.UseHangfireDashboard("/scheduler");
        }
    }
}