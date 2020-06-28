# Configuration

Exporter can be configured via configuration file, environment variables or command-line args.

## Configuration file

The file is written in YAML format, defined by the scheme described below. Brackets indicate that a parameter is optional. 

```html
serilog: <serilog-config>
exporter:
  elasticsearch:
    # Global Elasticsearch timeout
    [ request_timeout: <duration> | default: 60s ]
    
    connection:
      [ single_node : <single-node-connection-config> ]
      [ static_cluster : <static-cluster-connection-config> ]

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
      [ mode: <metrics-evaluation-mode> | default: Scheduled ]

  query_files:
    [- <file-path> ]
```

### `<serilog-config>`

Serilog configuration translated to yaml format. See [link](https://github.com/serilog/serilog-settings-configuration) for more details.

### `<single-node-connection-config>`

Configuration for connecting to single-node Elasticsearch
```
url: <string>
```

### `<static-cluster-connection-config>`

Configuration for connecting to static Elasticsearch cluster
```html
urls:
 - <string>
 - ...
 - <string>
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