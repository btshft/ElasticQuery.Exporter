using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Lib.File;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Models.Mappers;
using ElasticQuery.Exporter.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;

namespace ElasticQuery.Exporter.Services.QueryProvider
{
    public class FileMetricQueriesProvider : IMetricQueriesProvider
    {
        private readonly IOptions<ExporterOptions> _exporterOptionsProvider;
        private readonly IDeserializer _deserializer;
        private readonly Lazy<Task<IReadOnlyCollection<MetricQuery>>> _queriesProvider;
        private readonly ILogger<FileMetricQueriesProvider> _logger;
        private readonly IGlobFileProvider _fileProvider;

        public FileMetricQueriesProvider(
            IOptions<ExporterOptions> exporterOptionsProvider,
            IDeserializer deserializer,
            ILogger<FileMetricQueriesProvider> logger, 
            IGlobFileProvider fileProvider)
        {
            _exporterOptionsProvider = exporterOptionsProvider;
            _deserializer = deserializer;
            _logger = logger;
            _fileProvider = fileProvider;

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
            async Task ParseAsync(string path, IDictionary<string, MetricQuery> queryMetrics)
            {
                var content = await File.ReadAllTextAsync(path, Encoding.UTF8);
                var definition = _deserializer.Deserialize<MetricQueryDefinition>(content);

                if (queryMetrics.ContainsKey(definition.Name))
                    throw new Exception($"Attempt to insert duplicate query '{definition.Name}'");

                queryMetrics.Add(definition.Name, definition.ToQuery());
            }

            var options = _exporterOptionsProvider.Value;
            var queries = new Dictionary<string, MetricQuery>();
            var processedPath = new HashSet<string>(StringComparer.InvariantCulture);

            if (!options.QueryFiles.Any())
                return queries.Values;

            foreach (var queryFile in options.QueryFiles)
            {
                var filesPath = _fileProvider.GetFiles(queryFile).ToArray();
                if (!filesPath.Any())
                {
                    _logger.LogWarning($"No query files matched glob path '{queryFile}'");
                    continue;
                }

                _logger.LogInformation($"Glob pattern '{queryFile}' resolved to '{filesPath.Length}' files");

                foreach (var path in filesPath)
                {
                    _logger.LogInformation($"Start processing file '{path}'");

                    var exists = !processedPath.Add(path);
                    if (exists)
                    {
                        _logger.LogWarning($"File '{path}' has been processed before. Skipping it...");
                        continue;
                    }

                    await ParseAsync(path, queries);

                    _logger.LogInformation($"File '{path}' successfully processed");
                }
            }

            return queries.Values;
        }
    }
}