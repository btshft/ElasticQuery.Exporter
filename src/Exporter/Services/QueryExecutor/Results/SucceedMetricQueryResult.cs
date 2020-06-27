using System;

namespace ElasticQuery.Exporter.Services.QueryExecutor.Results
{
    public class SucceedMetricQueryResult : IMetricQueryResult
    {
        public long Hits { get; }

        public TimeSpan Duration { get; }

        public SucceedMetricQueryResult(long hits, TimeSpan duration)
        {
            Hits = hits;
            Duration = duration;
        }

        /// <inheritdoc />
        public bool IsSucceed { get; } = true;
    }
}