using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Services.QueryExecutor.Results;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ElasticQuery.Exporter.Services.MetricsWriter
{
    public class MetricsWriter : IMetricsWriter
    {
        private readonly IMetrics _metrics;
        private readonly ILogger<MetricsWriter> _logger;

        public MetricsWriter(IMetrics metrics, ILogger<MetricsWriter> logger)
        {
            _metrics = metrics;
            _logger = logger;
        }

        /// <inheritdoc />
        public Task WriteAsync(MetricQuery query, IMetricQueryResult result, CancellationToken cancellation = default)
        {
            MetricTags CreateTags(IDictionary<string, string> additionalLabels = null)
            {
                var labels = (IDictionary<string, string>)new Dictionary<string, string>
                {
                    { "query", query.Name }
                };

                if (query.Labels.Any())
                    labels = labels.MergeDifference(query.Labels);

                if (additionalLabels != null)
                    labels = labels.MergeDifference(additionalLabels);

                return new MetricTags(labels.Keys.ToArray(), labels.Values.ToArray());
            }

            void WriteSucceed(SucceedMetricQueryResult succeedResult)
            {
                var tags = CreateTags();

                _metrics.Measure.Gauge.SetValue(MetricsRegistry.Gauges.Hits, tags, succeedResult.Hits);
                _metrics.Measure.Gauge.SetValue(MetricsRegistry.Gauges.Duration, tags, succeedResult.Duration.TotalMilliseconds);

                if (succeedResult.ValueAggregations.Any())
                {
                    foreach (var aggregation in succeedResult.ValueAggregations)
                    {
                        if (!aggregation.Value.HasValue)
                            continue;

                        var aggregationTags = CreateTags(new Dictionary<string, string>
                        {
                            ["aggregation"] = aggregation.Key
                        });

                        _metrics.Measure.Gauge.SetValue(MetricsRegistry.Gauges.ValueAggregation, aggregationTags, aggregation.Value.Value);
                    }
                }
            }

            void WriteFailure(FailureMetricQueryResult failureResult)
            {
                var tags = CreateTags();

                if (failureResult.Timeout)
                    _metrics.Measure.Counter.Increment(MetricsRegistry.Counters.Timeouts, tags);

                if (failureResult.Exception != null)
                    _metrics.Measure.Counter.Increment(MetricsRegistry.Counters.Exceptions, tags);
            }

            void LogFailure(FailureMetricQueryResult failureResult)
            {
                if (failureResult.Timeout)
                    _logger.LogWarning("Query '{Query}' - evaluation timed out.", query.Name);

                if (failureResult.Exception != null)
                    _logger.LogError(failureResult.Exception, "Query '{Query}' - evaluation failed with exception. Server error: {Error}", 
                        query.Name, JsonConvert.SerializeObject(failureResult.ServerError));
            }

            switch (result)
            {
                case SucceedMetricQueryResult succeed:
                    WriteSucceed(succeed);
                    break;

                case FailureMetricQueryResult failure:
                    WriteFailure(failure);
                    LogFailure(failure);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}