using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Lib.Extension;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Services.QueryExecutor.Results;
using Microsoft.Extensions.Logging;
using Nest;

namespace ElasticQuery.Exporter.Services.QueryExecutor
{
    public class MetricQueryExecutor : IMetricQueryExecutor
    {
        private readonly ElasticClient _client;
        private readonly ILogger<MetricQueryExecutor> _logger;

        public MetricQueryExecutor(ElasticClient client, ILogger<MetricQueryExecutor> logger)
        {
            _client = client;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<IMetricQueryResult> ExecuteAsync(MetricQuery query, TimeSpan timeout, CancellationToken cancellation = default)
        {
            var result = await _client.SearchAsync<dynamic>(d =>
            {
                if (query.Indices.Any())
                {
                    d = d.Index(Indices.Index(query.Indices));
                }

                return d
                    .Source(enabled: false)
                    .Query(q => q
                    .Raw(query.Query));

            }, cancellation).WithTimeout(timeout);

            if (result.TimedOut)
            {
                return new FailureMetricQueryResult
                {
                    Timeout = result.TimedOut
                };
            }

            var queryResult = result.Result;
            if (queryResult.IsValid)
            {
                return new SucceedMetricQueryResult(
                    queryResult.HitsMetadata.Total.Value, 
                    TimeSpan.FromMilliseconds(queryResult.Took));
            }

            if (queryResult.OriginalException != null)
            {
                _logger.LogError(queryResult.OriginalException, "Exception while executing query '{Query}': {ErrorInfo}", 
                    query.Name, 
                    queryResult.ServerError?.ToString());
            }

            return new FailureMetricQueryResult
            {
                Exception = queryResult.OriginalException,
                ServerError = queryResult.ServerError,
                Terminated = queryResult.TerminatedEarly,
                Timeout = queryResult.TimedOut
            };
        }
    }
}