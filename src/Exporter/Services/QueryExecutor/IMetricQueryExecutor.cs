using System;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Services.QueryExecutor.Results;

namespace ElasticQuery.Exporter.Services.QueryExecutor
{
    public interface IMetricQueryExecutor
    {
        Task<IMetricQueryResult> ExecuteAsync(MetricQuery query, TimeSpan timeout, CancellationToken cancellation = default);
    }
}