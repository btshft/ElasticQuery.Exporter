using System;
using Elasticsearch.Net;

namespace ElasticQuery.Exporter.Services.QueryExecutor.Results
{
    public class FailureMetricQueryResult : IMetricQueryResult
    {
        public Exception Exception { get; set; }

        public bool Timeout { get; set; }

        public bool Terminated { get; set; }

        public ServerError ServerError { get; set; }

        /// <inheritdoc />
        public bool IsSucceed { get; } = false;
    }
}