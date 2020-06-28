using System;
using System.Collections.Generic;
using ElasticQuery.Exporter.Models;

namespace ElasticQuery.Exporter.Options
{
    public class ExporterOptions
    {
        public ElasticSearchOptions ElasticSearch { get; set; }

        public string[] QueryFiles { get; set; } = Array.Empty<string>();

        public MetricsOptions Metrics { get; set; } = new MetricsOptions();

        public class MetricsOptions
        {
            public EvaluationOptions Evaluation { get; set; } = new EvaluationOptions();

            public Dictionary<string, string> Labels { get; set; } 
                = new Dictionary<string, string>();

            public string Prefix { get; set; } = "elastic_query";

            public class EvaluationOptions
            {
                public MetricsEvaluationMode Mode { get; set; } = MetricsEvaluationMode.Scheduled;

                public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

                public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
            }
        }

        public class ElasticSearchOptions
        {
            public ConnectionOptions Connection { get; set; }

            public class ConnectionOptions
            {
                public SingleNodeOptions SingleNode { get; set; }

                public StaticClusterOptions StaticCluster { get; set; }

                public class SingleNodeOptions
                {
                    public string Url { get; set; }
                }

                public class StaticClusterOptions
                {
                    public string[] Urls { get; set; } = Array.Empty<string>();
                }
            }
        }
    }
}