using System;
using System.Collections.Generic;

namespace ElasticQuery.Exporter.Models
{
    public class MetricQueryDefinition
    {
        public MetricQueryType Type { get; set; }

        public string Name { get; set; }

        public TimeSpan? Timeout { get; set; }

        public TimeSpan? Interval { get; set; }

        public MetricsEvaluationMode? EvaluationMode { get; set; }

        public List<string> Indices { get; set; }
            = new List<string>();

        public Dictionary<string, string> Labels { get; set; }
            = new Dictionary<string, string>();

        #region Default Query
        public string Request { get; set; }

        #endregion

        # region Raw Query

        public MetricQuerySlidingDate SlidingDate { get; set; }

        public string Query { get; set; }

        #endregion
    }
}