# Prometheus Elasticsearch query exporter [![Docker Image Version (latest semver)](https://img.shields.io/docker/v/bitshift/elastic-query-exporter?sort=semver)](https://hub.docker.com/r/bitshift/elastic-query-exporter)

The exporter runs Elasticsearch queries and transforms query results to Prometheus metrics.

[![Release](https://github.com/btshft/ElasticQuery.Exporter/workflows/Release/badge.svg)](https://github.com/btshft/ElasticQuery.Exporter/actions?query=workflow%3ARelease) [![Development](https://github.com/btshft/ElasticQuery.Exporter/workflows/Development/badge.svg)](https://github.com/btshft/ElasticQuery.Exporter/actions?query=workflow%3ADevelopment)

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


## Usage 
// TODO

### Examples
// TODO
