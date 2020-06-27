using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using ElasticQuery.Exporter.Services.MetricsWriter;
using ElasticQuery.Exporter.Services.QueryExecutor;
using ElasticQuery.Exporter.Services.QueryProvider;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElasticQuery.Exporter.Jobs
{
    public class QueryEvaluationBackgroundService : BackgroundService
    {
        private readonly IMetricQueriesProvider _queriesProvider;
        private readonly IMetricQueryExecutor _queryExecutor;
        private readonly IMetricsWriter _metricsWriter;
        private readonly ILogger<QueryEvaluationBackgroundService> _logger;
        private readonly Random _random;
        private readonly IOptions<ExporterOptions> _optionsProvider;

        public QueryEvaluationBackgroundService(
            IMetricQueriesProvider queriesProvider, 
            IMetricQueryExecutor queryExecutor, 
            IMetricsWriter metricsWriter,
            ILogger<QueryEvaluationBackgroundService> logger, 
            IOptions<ExporterOptions> optionsProvider)
        {
            _queriesProvider = queriesProvider;
            _queryExecutor = queryExecutor;
            _metricsWriter = metricsWriter;
            _logger = logger;
            _optionsProvider = optionsProvider;
            _random = new Random();
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queries = await _queriesProvider.GetAsync(stoppingToken);
            var tasks = queries.Select(q =>
            {
                return Task.Run(async () =>
                {
                    var options = _optionsProvider.Value;
                    var period = q.Interval.GetValueOrDefault(options.Metrics.Evaluation.Interval);
                    var timeout = q.Timeout.GetValueOrDefault(options.Metrics.Evaluation.Timeout);

                    var delay = TimeSpan.FromSeconds(_random.Next(0, 2));
                    await Task.Delay(delay, stoppingToken);

                    await ScheduleQueryAsync(q, period, timeout, stoppingToken);

                }, stoppingToken);
            });

            await Task.WhenAll(tasks);
        }

        private Task ScheduleQueryAsync(MetricQuery query, TimeSpan period, TimeSpan timeout, CancellationToken cancellation = default)
        {
            return Task.Run(async () =>
            {
                while (!cancellation.IsCancellationRequested)
                {
                    var beforeExecution = DateTime.UtcNow;

                    try
                    {
                        var result = await _queryExecutor.ExecuteAsync(query, timeout, cancellation);
                        await _metricsWriter.WriteAsync(query, result, cancellation);

                        var afterExecution = DateTime.UtcNow;
                        var executionDuration = afterExecution - beforeExecution;

                        if (cancellation.IsCancellationRequested)
                            return;

                        if (period > executionDuration)
                        {
                            var waitTime = period - executionDuration;
                            await Task.Delay(waitTime, cancellation);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Ignore
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Exception occured while processing query '{Query}'", query.Name);
                    }
                }
            }, cancellation);
        }
    }
}