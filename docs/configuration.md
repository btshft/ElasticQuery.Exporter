# Configuration

Exporter can be configured via configuration file, environment variables or command-line args.

## Configuration file

The file is written in YAML format, defined by the scheme described below. Brackets indicate that a parameter is optional. 

```html
serilog: <serilog_config>
exporter:
  elasticsearch:
    # Global Elasticsearch timeout
    [ request_timeout: <duration> | default: 60s ]
    
    connection:
      [ single_node : <single_node_connection_config> ]
      [ static_cluster : <static_cluster_connection_config> ]

  metrics: 
    # Metrics name prefix
    [ prefix: <string> | default: "elastic_query" ]

    # Global labels
    labels:
      [ - <name>: <value> ... ]

    evaluation:
      # How often queries are evaluated by default (ignored for on-demand mode)
      [ interval: <duration> | default: 30s ]

      # Default query timeout
      [ timeout: <duration> | default 30s ]

      # Defaul query evaluation mode
      [ mode: <metrics_evaluation_mode> | default: Scheduled ]

  query_files:
    [- <file_path> ]
```

### `<serilog_config>`

Serilog configuration translated to yaml format. See [link](https://github.com/serilog/serilog-settings-configuration) for more details.

### `<single_node_connection_config>`

Configuration for connecting to single-node Elasticsearch
```
url: <string>
```

### `<static_cluster_connection_config>`

Configuration for connecting to static Elasticsearch cluster
```
urls:
 - <string_1>
 - ...
 - <string_n>
```

## Minimal example

```yaml
serilog:
  minimum_level:
    default: "Error"

exporter:
  elasticsearch:
    connection:
      singlenode:
        url: http://elasticsearch:9200

  query_files:
  - "/app/queries/errors-query.yaml"
  - "/app/queries/login-failed-events-query.yaml"
```