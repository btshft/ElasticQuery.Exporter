using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;

namespace ElasticQuery.Exporter.Services.MetricsWriter
{
    public static class MetricsRegistry
    {
        public static class Gauges
        {
            public static readonly GaugeOptions Hits = new GaugeOptions
            {
                Name = "hits",
                MeasurementUnit = Unit.Custom("hits"),
                ResetOnReporting = true
            };

            public static readonly GaugeOptions Duration = new GaugeOptions
            {
                Name = "duration_milliseconds",
                ResetOnReporting = true,
                MeasurementUnit = Unit.Custom("milliseconds")
            };
        }

        public static class Counters
        {
            public static readonly CounterOptions Exceptions = new CounterOptions
            {
                Name = "exceptions_total",
                ResetOnReporting = false,
            };

            public static readonly CounterOptions Timeouts = new CounterOptions
            {
                Name = "timeouts_total",
                ResetOnReporting = false
            };
        }
    }
}