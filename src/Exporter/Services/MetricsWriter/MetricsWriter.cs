using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;
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
            void WriteSucceed(SucceedMetricQueryResult succeedResult)
            {
                var hitsGauge = new GaugeOptions
                {
                    Name = $"{query.Name}_hits",
                    MeasurementUnit = Unit.Custom("hits"),
                    ResetOnReporting = true
                };

                var durationGauge = new GaugeOptions
                {
                    Name = $"{query.Name}_duration_ms",
                    ResetOnReporting = true
                };

                var tags = query.Labels.Any()
                    ? new MetricTags(query.Labels.Keys.ToArray(), query.Labels.Values.ToArray())
                    : new MetricTags();

                _metrics.Measure.Gauge.SetValue(durationGauge, tags, succeedResult.Duration.TotalMilliseconds);
                _metrics.Measure.Gauge.SetValue(hitsGauge, tags, succeedResult.Hits);
            }

            void WriteFailure(FailureMetricQueryResult failureResult)
            {
                var exceptionsCounter = new CounterOptions
                {
                    Name = $"{query.Name}_exceptions",
                    ResetOnReporting = true
                };

                var timeoutsCounter = new CounterOptions
                {
                    Name = $"{query.Name}_timeouts",
                    ResetOnReporting = true
                };

                var tags = query.Labels.Any()
                    ? new MetricTags(query.Labels.Keys.ToArray(), query.Labels.Values.ToArray())
                    : new MetricTags();

                if (failureResult.Timeout)
                    _metrics.Measure.Counter.Increment(timeoutsCounter, tags);

                if (failureResult.Exception != null)
                    _metrics.Measure.Counter.Increment(exceptionsCounter, tags);
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