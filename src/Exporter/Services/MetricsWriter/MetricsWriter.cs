using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Services.QueryExecutor.Results;

namespace ElasticQuery.Exporter.Services.MetricsWriter
{
    public class MetricsWriter : IMetricsWriter
    {
        private readonly IMetrics _metrics;

        public MetricsWriter(IMetrics metrics)
        {
            _metrics = metrics;
        }

        /// <inheritdoc />
        public Task WriteAsync(MetricQuery query, IMetricQueryResult result, CancellationToken cancellation = default)
        {
            MetricTags CreateTags()
            {
                var labels = (IDictionary<string, string>)new Dictionary<string, string>
                {
                    { "query", query.Name }
                };

                if (query.Labels.Any())
                    labels = labels.MergeDifference(query.Labels);

                return new MetricTags(labels.Keys.ToArray(), labels.Values.ToArray());
            }

            void WriteSucceed(SucceedMetricQueryResult succeedResult)
            {
                var tags = CreateTags();

                _metrics.Measure.Gauge.SetValue(MetricsRegistry.Gauges.Hits, tags, succeedResult.Hits);
                _metrics.Measure.Gauge.SetValue(MetricsRegistry.Gauges.Duration, tags, succeedResult.Duration.TotalMilliseconds);
            }

            void WriteFailure(FailureMetricQueryResult failureResult)
            {
                var tags = CreateTags();

                if (failureResult.Timeout)
                    _metrics.Measure.Counter.Increment(MetricsRegistry.Counters.Timeouts, tags);

                if (failureResult.Exception != null)
                    _metrics.Measure.Counter.Increment(MetricsRegistry.Counters.Exceptions, tags);
            }

            switch (result)
            {
                case SucceedMetricQueryResult succeed:
                    WriteSucceed(succeed);
                    break;

                case FailureMetricQueryResult failure:
                    WriteFailure(failure);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}