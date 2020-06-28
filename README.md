# Prometheus Elasticsearch query exporter 

The exporter runs Elasticsearch queries and transforms query results to Prometheus metrics.

[![Docker Image Version (latest semver)](https://img.shields.io/docker/v/bitshift/elastic-query-exporter?sort=semver)](https://hub.docker.com/r/bitshift/elastic-query-exporter) [![Docs](https://img.shields.io/badge/docs-master-blue)](https://github.com/btshft/ElasticQuery.Exporter/tree/master/docs)

## Queries

Queries are defined in own files similar to how alerts rules are defined in Prometheus.

They are divided into two types:
* **Scheduled** queries are evaluated at a certain intervals in background. When Prometheus comes for metrics the last evaluated result retured.
* **On-demand** queries are evaluated on scrape request.

Query results automatically turn into a series of metrics. 

### Metrics

Name | Description | Type | Labels
-----|-------------|------|-------
`elastic_query_hits` | Number of records satisfied by query | Gauge | query 
`elastic_query_duration_milliseconds` | Query execution Time in milliseconds | Gauge | query
`elastic_query_exceptions_total` | Total number of exceptions occured on query evaluation | Counter | query
`elastic_query_timeouts_total` | Total number of timeouts occured on query evaluation | Counter | query

### Aggregations support
Aggregations are not supported currently.


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
