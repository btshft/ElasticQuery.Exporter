using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Models;

namespace ElasticQuery.Exporter.Services.QueryProvider
{
    public interface IMetricQueriesProvider
    {
        Task<IReadOnlyCollection<MetricQuery>> GetAsync(CancellationToken cancellation = default);
    }
}
