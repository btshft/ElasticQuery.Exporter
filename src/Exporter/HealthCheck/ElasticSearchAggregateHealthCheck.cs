using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Options;
using HealthChecks.Elasticsearch;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElasticQuery.Exporter.HealthCheck
{
    public class ElasticSearchAggregateHealthCheck : IHealthCheck
    {
        private readonly Dictionary<string, ElasticsearchHealthCheck> _healthChecks;
        private readonly ILogger<ElasticSearchAggregateHealthCheck> _logger;

        public ElasticSearchAggregateHealthCheck(IOptions<ExporterOptions> optionsProvider, ILogger<ElasticSearchAggregateHealthCheck> logger)
        {
            _logger = logger;
            var urls = new List<string>();
            var options = optionsProvider.Value;

            if (options.ElasticSearch.Connection.SingleNode != null)
            {
                urls.Add(options.ElasticSearch.Connection.SingleNode.Url);
            } 
            else if (options.ElasticSearch.Connection.StaticCluster != null)
            {
                urls.AddRange(options.ElasticSearch.Connection.StaticCluster.Urls);
            }

            _healthChecks = urls.Select(u =>
            {
                var esOptions = new ElasticsearchOptions()
                    .UseServer(u);

                return new KeyValuePair<string, ElasticsearchHealthCheck>(u,
                    new ElasticsearchHealthCheck(esOptions));

            }).ToDictionary(
                k => k.Key,
                v => v.Value);
        }

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var results = new Dictionary<string, HealthCheckResult>();
            var data = new Dictionary<string, object>();

            foreach (var (url, check) in _healthChecks)
            {
                try
                {
                    var result = await check.CheckHealthAsync(context, cancellationToken);

                    results[url] = result;
                    data[url] = new
                    {
                        status = result.Status.ToString(),
                        desciption = result.Description
                    };
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception occured while performing health-check on Elasticsearch '{ES}'", url);
                    results[url] = HealthCheckResult.Degraded("Exceptions occured", e);
                }
            }

            var isDegraded = results.Values.Any(r => r.Status != HealthStatus.Healthy);

            return isDegraded
                ? HealthCheckResult.Degraded(data: data)
                : HealthCheckResult.Healthy(data: data);
        }
    }
}
