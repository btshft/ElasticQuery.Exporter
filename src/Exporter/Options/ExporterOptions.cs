using System;
using System.Collections.Generic;

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
                public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

                public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
            }
        }

        public class ElasticSearchOptions
        {
            public ConnectionOptions Connection { get; set; }

            public class ConnectionOptions
            {
                public SingleNodeOptions SingleNode { get; set; }

                public class SingleNodeOptions
                {
                    public string Url { get; set; }
                }
            }
        }
    }
}