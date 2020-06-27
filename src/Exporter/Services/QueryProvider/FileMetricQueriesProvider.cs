using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using FluentValidation;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;

namespace ElasticQuery.Exporter.Services.QueryProvider
{
    public class FileMetricQueriesProvider : IMetricQueriesProvider
    {
        private readonly IOptions<ExporterOptions> _exporterOptionsProvider;
        private readonly IDeserializer _deserializer;
        private readonly IValidator<MetricQuery> _queryValidator;
        private readonly Lazy<Task<IReadOnlyCollection<MetricQuery>>> _queriesProvider;

        public FileMetricQueriesProvider(IOptions<ExporterOptions> exporterOptionsProvider, IDeserializer deserializer, IValidator<MetricQuery> queryValidator)
        {
            _exporterOptionsProvider = exporterOptionsProvider;
            _deserializer = deserializer;
            _queryValidator = queryValidator;

            _queriesProvider = new Lazy<Task<IReadOnlyCollection<MetricQuery>>>(CreateQueries);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<MetricQuery>> GetAsync(CancellationToken cancellation = default)
        {
            var queries = _queriesProvider.Value;
            return await queries;
        }

        private async Task<IReadOnlyCollection<MetricQuery>> CreateQueries()
        {
            var options = _exporterOptionsProvider.Value;
            var queries = new Dictionary<string, MetricQuery>();

            if (options.QueryFiles.Any())
            {
                foreach (var queryFile in options.QueryFiles)
                {
                    if (!File.Exists(queryFile))
                        throw new Exception($"Query file '{queryFile}' not exists");

                    var content = await File.ReadAllTextAsync(queryFile, Encoding.UTF8);
                    var query = _deserializer.Deserialize<MetricQuery>(content);

                    if (queries.ContainsKey(query.Name))
                        throw new Exception($"Attempt to insert duplicate query '{query.Name}'");

                    var result = await _queryValidator.ValidateAsync(query);
                    if (!result.IsValid)
                        throw new Exception($"Query '{query.Name}' is invalid: {result}");

                    for (var i = 0; i < query.Indices.Count; i++)
                        query.Indices[i] = string.Format(query.Indices[i], DateTime.UtcNow);

                    queries.Add(query.Name, query);
                }
            }

            return queries.Values;
        }
    }
}