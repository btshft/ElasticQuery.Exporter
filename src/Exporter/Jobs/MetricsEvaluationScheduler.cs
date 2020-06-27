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
    public class MetricsEvaluationScheduler : BackgroundService
    {
        private readonly IMetricQueriesProvider _queriesProvider;
        private readonly IMetricQueryExecutor _queryExecutor;
        private readonly IMetricsWriter _metricsWriter;
        private readonly ILogger<MetricsEvaluationScheduler> _logger;
        private readonly Random _random;
        private readonly IOptions<ExporterOptions> _optionsProvider;

        public MetricsEvaluationScheduler(
            IMetricQueriesProvider queriesProvider, 
            IMetricQueryExecutor queryExecutor, 
            IMetricsWriter metricsWriter,
            ILogger<MetricsEvaluationScheduler> logger, 
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
                var options = _optionsProvider.Value;
                var defaultEvaluationMode = options.Metrics.Evaluation.Mode;

                if (q.EvaluationMode == MetricsEvaluationMode.OnDemand)
                {
                    _logger.LogInformation($"Query '{q.Name}' background execution skipped because explicit on-demand evaluation mode set.");
                    return Task.CompletedTask;
                }

                if (defaultEvaluationMode == MetricsEvaluationMode.OnDemand &&
                    !q.EvaluationMode.HasValue)
                {
                    _logger.LogInformation($"Query '{q.Name}' background execution skipped because default execution mode is 'on-demand' and no explicit mode set on query.");
                    return Task.CompletedTask;
                }

                return Task.Run(async () =>
                {
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
                        _logger.LogInformation($"Executing scheduled query '{query.Name}'");

                        var result = await _queryExecutor.ExecuteAsync(query, timeout, cancellation);
                        await _metricsWriter.WriteAsync(query, result, cancellation);

                        _logger.LogInformation($"Executed scheduled query '{query.Name}'");

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
                    catch (OperationCanceledException)
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