using System;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using ElasticQuery.Exporter.Services.MetricsWriter;
using ElasticQuery.Exporter.Services.QueryExecutor;
using Hangfire;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElasticQuery.Exporter.Scheduler
{
    public class ScheduledMetricsEvaluator
    {
        private readonly ILogger<ScheduledMetricsEvaluator> _logger;
        private readonly IMetricQueryExecutor _queryExecutor;
        private readonly IMetricsWriter _metricsWriter;
        private readonly IOptions<ExporterOptions> _optionsProvider;
        private readonly IBackgroundJobClient _jobClient;

        public ScheduledMetricsEvaluator(
            ILogger<ScheduledMetricsEvaluator> logger, 
            IMetricQueryExecutor queryExecutor, 
            IMetricsWriter metricsWriter, 
            IOptions<ExporterOptions> optionsProvider, 
            IBackgroundJobClient jobClient)
        {
            _logger = logger;
            _queryExecutor = queryExecutor;
            _metricsWriter = metricsWriter;
            _optionsProvider = optionsProvider;
            _jobClient = jobClient;
        }

        public async Task EvaluateAsync(MetricQuery query, CancellationToken cancellation = default)
        {
            var options = _optionsProvider.Value;

            var timeout = query.Timeout.GetValueOrDefault(options.Metrics.Evaluation.Timeout);
            var interval = query.Interval.GetValueOrDefault(options.Metrics.Evaluation.Interval);

            try
            {
                _logger.LogInformation("Starting evaluation of query '{Query}'", query.Name);

                var result = await _queryExecutor.ExecuteAsync(query, timeout, cancellation);
                await _metricsWriter.WriteAsync(query, result, cancellation);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Query '{Query}' evaluation failed with exception", query.Name);
            }
            finally
            {
                _logger.LogInformation("Query '{Query}' evaluated. Next evaluation on '{NextOccurence}'", query.Name, (DateTime.Now + interval));

                // Re-schedule
                _jobClient.Schedule<ScheduledMetricsEvaluator>(
                    e => e.EvaluateAsync(query, cancellation), interval);
            }
        }
    }
}