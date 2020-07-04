using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using ElasticQuery.Exporter.Services.QueryProvider;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElasticQuery.Exporter.Jobs
{
    public class ScheduledMetricsInitializer : IHostedService
    {
        private readonly IMetricQueriesProvider _queriesProvider;
        private readonly IOptions<ExporterOptions> _optionsProvider;
        private readonly IBackgroundJobClient _jobClient;
        private readonly ILogger<ScheduledMetricsInitializer> _logger;

        public ScheduledMetricsInitializer(
            IMetricQueriesProvider queriesProvider, 
            IOptions<ExporterOptions> optionsProvider, 
            IBackgroundJobClient jobClient, 
            ILogger<ScheduledMetricsInitializer> logger)
        {
            _queriesProvider = queriesProvider;
            _optionsProvider = optionsProvider;
            _jobClient = jobClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = _optionsProvider.Value;

            var allQueries = await _queriesProvider.GetAsync(cancellationToken);
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
                var interval = query.Interval.GetValueOrDefault(options.Metrics.Evaluation.Interval);

                _jobClient.Schedule<ScheduledMetricsEvaluator>(
                    e => e.EvaluateAsync(queryRef, cancellationToken), interval);

                _logger.LogInformation("Query '{Query}' scheduled at '{Occurence}'", query.Name, (DateTime.Now + interval));
            }
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}