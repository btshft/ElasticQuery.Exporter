using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ElasticQuery.Exporter.Lib.Extension;
using ElasticQuery.Exporter.Models;
using ElasticQuery.Exporter.Options;
using ElasticQuery.Exporter.Services.QueryExecutor.Results;
using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using Nest;
using TaskExtensions = ElasticQuery.Exporter.Lib.Extension.TaskExtensions;

namespace ElasticQuery.Exporter.Services.QueryExecutor
{
    public class MetricQueryExecutor : IMetricQueryExecutor
    {
        private readonly ElasticClient _client;
        private readonly IOptions<ExporterOptions> _optionsProvider;

        public MetricQueryExecutor(ElasticClient client, IOptions<ExporterOptions> optionsProvider)
        {
            _client = client;
            _optionsProvider = optionsProvider;
        }

        /// <inheritdoc />
        public async Task<IMetricQueryResult> ExecuteAsync(MetricQuery query, TimeSpan timeout, CancellationToken cancellation = default)
        {
            var result = query.Type switch
            {
                MetricQueryType.Default => ExecuteDefaultAsync((DefaultMetricQuery) query, timeout, cancellation),
                MetricQueryType.Raw => ExecuteRawAsync((RawMetricQuery) query, timeout, cancellation),
                _ => throw new InvalidOperationException($"Query type '{query.Type}' not supported")
            };

            return await result;
        }

        private async Task<IMetricQueryResult> ExecuteRawAsync(RawMetricQuery query, TimeSpan timeout,
            CancellationToken cancellation = default)
        {
            var indices = GetIndices(query);
            var indicesString = string.Join(",", indices);

            var result = await _client.LowLevel.SearchAsync<SearchResponse<dynamic>>(indicesString, body: query.Request,
                new SearchRequestParameters
                {
                    AllowNoIndices = false,
                    TrackTotalHits = true,
                    ExpandWildcards = ExpandWildcards.Open
                }, cancellation).WithTimeout(timeout);

            return ConvertResult<dynamic, SearchResponse<dynamic>>(result);
        }

        private async Task<IMetricQueryResult> ExecuteDefaultAsync(DefaultMetricQuery query, TimeSpan timeout, 
            CancellationToken cancellation = default)
        {
            var result = await _client.SearchAsync<dynamic>(d =>
            {
                return d
                    .Index(Indices.Index(GetIndices(query)))
                    .Source(enabled: false)
                    .Query(q =>
                    {
                        var esQuery = q.Raw(query.Query);

                        if (query.SlidingDate != null)
                        {
                            var to = DateMath.Now;
                            var from = DateMath.FromString($"now-{query.SlidingDate.Range}");

                            var slidingDateQuery = q.Bool(b => b
                                .Filter(f => f
                                    .Bool(bf => bf
                                        .Must(m => m
                                            .DateRange(r => r
                                                .Field(query.SlidingDate.Field)
                                                .LessThan(to)
                                                .GreaterThanOrEquals(from))))));

                            esQuery = esQuery && slidingDateQuery;
                        }

                        return esQuery;
                    });

            }, cancellation).WithTimeout(timeout);

            return ConvertResult<dynamic, ISearchResponse<dynamic>>(result);
        }

        private IEnumerable<string> GetIndices(MetricQuery query)
        {
            var indices = new List<string>();
            var options = _optionsProvider.Value.ElasticSearch;

            if (options.DefaultIndex != null)
                indices.Add(string.Format(options.DefaultIndex, DateTime.UtcNow));

            if (query.Indices.Any())
                indices.AddRange(query.Indices.Select(i => string.Format(i, DateTime.UtcNow)));

            return indices;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IMetricQueryResult ConvertResult<TInner, TResult>(TaskExtensions.MaybeTimeout<TResult> result)
            where TResult : ISearchResponse<TInner>
            where TInner : class
        {
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
                var valueAggregations = new List<ValueAggregationResult>();
                if (queryResult.Aggregations != null)
                {
                    foreach (var (name, aggregation) in queryResult.Aggregations)
                    {
                        if (aggregation is ValueAggregate valueAggregate)
                            valueAggregations.Add(new ValueAggregationResult(name, valueAggregate.Value));
                    }
                }

                return new SucceedMetricQueryResult(
                    queryResult.HitsMetadata.Total.Value,
                    TimeSpan.FromMilliseconds(queryResult.Took), 
                    valueAggregations);
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