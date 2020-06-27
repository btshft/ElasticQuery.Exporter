using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Services.QueryExecutor.Results;

namespace ElasticQuery.Exporter.Services.MetricsWriter
{
    public interface IMetricsWriter
    {
        Task WriteAsync(MetricQuery query, IMetricQueryResult result, CancellationToken cancellation = default);
    }
}