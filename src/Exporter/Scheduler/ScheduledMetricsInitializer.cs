using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.HealthCheck;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using ElasticQuery.Exporter.Services.QueryProvider;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElasticQuery.Exporter.Scheduler
{
    public class ScheduledMetricsInitializer : BackgroundService
    {
        private readonly IMetricQueriesProvider _queriesProvider;
        private readonly IOptions<ExporterOptions> _optionsProvider;
        private readonly IBackgroundJobClient _jobClient;
        private readonly ILogger<ScheduledMetricsInitializer> _logger;
        private readonly MetricsSchedulerInitializedHealthCheck _healthCheck;

        public ScheduledMetricsInitializer(
            IMetricQueriesProvider queriesProvider, 
            IOptions<ExporterOptions> optionsProvider, 
            IBackgroundJobClient jobClient, 
            ILogger<ScheduledMetricsInitializer> logger,
            MetricsSchedulerInitializedHealthCheck healthCheck)
        {
            _queriesProvider = queriesProvider;
            _optionsProvider = optionsProvider;
            _jobClient = jobClient;
            _logger = logger;
            _healthCheck = healthCheck;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = _optionsProvider.Value;

            var allQueries = await _queriesProvider.GetAsync(stoppingToken);
            var schedulerQueries = allQueries.Where(q =>
            {
                if (q.EvaluationMode == MetricsEvaluationMode.Scheduled)
                    return true;

                if (q.EvaluationMode == null && options.Metrics.Evaluation.Mode == MetricsEvaluationMode.Scheduled)
                    return true;

                return false;
            });

            foreach (var query in schedulerQueries)
            {
                var queryRef = query;

                _jobClient.Schedule<ScheduledMetricsEvaluator>(
                    e => e.EvaluateAsync(queryRef, stoppingToken), TimeSpan.Zero);

                _logger.LogInformation("Query '{Query}' successfully scheduled", query.Name);
            }

            _healthCheck.SchedulerInitialized = true;
        }
    }
}