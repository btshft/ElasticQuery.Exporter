using System;
using System.Collections.Generic;
using System.Linq;

namespace ElasticQuery.Exporter.Services.QueryExecutor.Results
{
    public class SucceedMetricQueryResult : IMetricQueryResult
    {
        public long Hits { get; }

        public TimeSpan Duration { get; }

        public SucceedMetricQueryResult(long hits, TimeSpan duration, IEnumerable<ValueAggregationResult> valueAggregations)
        {
            Hits = hits;
            Duration = duration;
            ValueAggregations = valueAggregations.ToArray();
        }

        /// <inheritdoc />
        public bool IsSucceed { get; } = true;

        public IReadOnlyCollection<ValueAggregationResult> ValueAggregations { get; }
    }
}