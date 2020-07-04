# Prometheus Elasticsearch query exporter 

The exporter runs Elasticsearch queries and transforms query results to Prometheus metrics.

[![Docker Image Version (latest semver)](https://img.shields.io/docker/v/bitshift/elastic-query-exporter?sort=semver)](https://hub.docker.com/r/bitshift/elastic-query-exporter) [![Docs](https://img.shields.io/badge/docs-master-blue)](https://github.com/btshft/ElasticQuery.Exporter/tree/master/docs)

## Queries

Queries are defined in own files similar to how alerts rules are defined in Prometheus.

They are divided by evaluation mode into two types:
* **Scheduled** queries are evaluated at a certain intervals in background. When Prometheus comes for metrics the last evaluated result retured.
* **On-demand** queries are evaluated on scrape request.

Also queries are divided by type:
* **Raw** - contains raw Elasticsearch request with minimal configuration.
* **Default** - contains only `query` Elasticsearch request part but provides more convenient configuration options.

Query results automatically turn into a series of metrics. 

### Metrics

Name | Description | Type | Labels
-----|-------------|------|-------
`elastic_query_hits` | Number of records satisfied by query | Gauge | query 
`elastic_query_duration_milliseconds` | Query execution Time in milliseconds | Gauge | query
`elastic_query_exceptions_total` | Total number of exceptions occured on query evaluation | Counter | query
`elastic_query_timeouts_total` | Total number of timeouts occured on query evaluation | Counter | query
`elastic_query_value_aggregation` | Elasticsearch single-value aggregation result | Counter | query, aggregation

### Aggregations support

Aggregations partially supported **only for raw queries**. Supported single-value aggregations:
1. [Min](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-min-aggregation.html)
2. [Max](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-max-aggregation.html)
3. [Sum](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-sum-aggregation.html)
4. [Cardinality](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-cardinality-aggregation.html)
5. [Average](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-avg-aggregation.html)
6. [ValueCount](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-valuecount-aggregation.html)
7. [AverageBucket](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-avg-bucket-aggregation.html)
8. [Derivative](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-derivative-aggregation.html)
9. [SumBucket](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-sum-bucket-aggregation.html)
10. [MovingAverage](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-movavg-aggregation.html)
11. [CumulativeSum](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-cumulative-sum-aggregation.html)
12. [CumulativeCardinality](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-cumulative-cardinality-aggregation.html)
13. [BucketScript](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-bucket-script-aggregation.html)
14. [SerialDifferencing](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-serialdiff-aggregation.html)
15. [WeightedAverage](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-weight-avg-aggregation.html)
16. [MaxBucket](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-max-bucket-aggregation.html) (Limited: keys are not exposed as metric part)
17. [MinBucket](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-pipeline-min-bucket-aggregation.html) (Limited: keys are not exposed as metric part)
18. [MedianAbsoluteDeviation](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-median-absolute-deviation-aggregation.html)
19. [T-Test](https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations-metrics-ttest-aggregation.html)


## Quickstart 

1. Create configuration file `appsettings.Release.yaml` and query files.
2. Run exporter as docker container

Replace:
* `${CONFIG_PATH}` with path to `appsettings.Release.yaml`;
* `${QUERIES_PATH}` with path to queries directory;
* `${HOST_PORT}` with desired host port where you want to run exporter.

```
docker run --rm --name es-query-exporter \
   -v ${CONFIG_PATH}:/app/appsettings.Release.yaml \
   -v ${QUERIES_PATH}:/app/queries -p ${HOST_PORT}:80 \
   -e ASPNETCORE_ENVIRONMENT=Release \
   bitshift/elastic-query-exporter:latest
```

> Note: with the presented startup format, query files will be available at `app/queries` dir, so in configuration file same file path should be specified.

Other launch formats and configuration files are presented in the [examples](https://github.com/btshft/ElasticQuery.Exporter/tree/master/examples). 
See [documentation](https://github.com/btshft/ElasticQuery.Exporter/tree/master/docs) for configuration details.


## Endpoints

Exporter provides several endpoints:

1. `/metrics` - Prometheus metrics endpoint
2. `/scheduler` - Scheduler dashboard. 
3. `/health/live` - Liveness probe.
4. `/health/ready` - Readiness probe.
