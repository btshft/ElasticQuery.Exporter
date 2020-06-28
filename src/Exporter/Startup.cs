using System;
using System.Linq;
using App.Metrics;
using App.Metrics.Formatters.Prometheus;
using ElasticQuery.Exporter.Jobs;
using ElasticQuery.Exporter.Lib.Extension;
using ElasticQuery.Exporter.Middleware;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using ElasticQuery.Exporter.Services.MetricsWriter;
using ElasticQuery.Exporter.Services.QueryExecutor;
using ElasticQuery.Exporter.Services.QueryProvider;
using ElasticQuery.Exporter.Validators;
using Elasticsearch.Net;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

                var settings = new ConnectionSettings(pool);

                return new ElasticClient(settings);
            });

            services.AddTransient<IValidator<MetricQuery>, MetricQueryValidator>();
            services.AddTransient<IValidator<ExporterOptions>, ExporterOptionsValidator>();

            services.AddSingleton<IMetricQueriesProvider, FileMetricQueriesProvider>();
            services.AddSingleton<IMetricQueryExecutor, MetricQueryExecutor>();
            services.AddSingleton<IMetricsWriter, MetricsWriter>();

            services.AddHostedService<MetricsEvaluationScheduler>();
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
                    .UseMiddleware<MetricsEvaluationMiddleware>()
                    .UseMetricsEndpoint();

                endpoints.MapGet("/metrics", builder.Build());
            });
        }
    }
}