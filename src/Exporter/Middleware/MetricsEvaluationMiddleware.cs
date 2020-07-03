using System;
using System.Linq;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using ElasticQuery.Exporter.Services.MetricsWriter;
using ElasticQuery.Exporter.Services.QueryExecutor;
using ElasticQuery.Exporter.Services.QueryProvider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElasticQuery.Exporter.Middleware
{
    public class MetricsEvaluationMiddleware
    {
        private readonly RequestDelegate _next;

        public MetricsEvaluationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, 
            IMetricQueriesProvider queriesProvider, 
            IOptions<ExporterOptions> optionsProvider,
            IMetricQueryExecutor queryExecutor,
            IMetricsWriter metricsWriter,
            ILogger<MetricsEvaluationMiddleware> logger)
        {
            var options = optionsProvider.Value;
            var defaultMode = options.Metrics.Evaluation.Mode;

            var queries = await queriesProvider.GetAsync(context.RequestAborted);

            var tasks = queries.Select(q =>
            {
                if (q.EvaluationMode == MetricsEvaluationMode.Scheduled)
                    return Task.CompletedTask;

                if (!q.EvaluationMode.HasValue && defaultMode == MetricsEvaluationMode.Scheduled)
                    return Task.CompletedTask;

                return Task.Run(async () =>
                {
                    try
                    {
                        logger.LogInformation($"Executing on-demand query '{q.Name}'");

                        var timeout = q.Timeout.GetValueOrDefault(options.Metrics.Evaluation.Timeout);
                        var result = await queryExecutor.ExecuteAsync(q, timeout, context.RequestAborted);

                        await metricsWriter.WriteAsync(q, result, context.RequestAborted);

                        logger.LogInformation($"Executed on-demand query '{q.Name}'");
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Unable to write metric for query '{q.Name}'");
                    }

                }, context.RequestAborted);
            });

            await Task.WhenAll(tasks);
            await _next(context);
        }
    }
}
