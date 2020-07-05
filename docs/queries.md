# Queries

The metrics provided by the program are determined by queries. Queries can be evaluated on schedule or on demand. Query definition 
consists of raw JSON Elasticsearch query and additional metadata and parameters.

## Query definition

Queries are defined in YAML format. File schema described below.

Generic placeholders are defined as follows:
* `<es-index-format>` - elasticsearch index name. It can be specified as date-template, which will be rendered on query evaluation. For example `app-logs-{0:yyyy.MM.dd}`.
* `<es-json-query>` - elasticsearch query part. Should be valid JSON. 
* `<es-json-request>` - elasticsearch request. Should be valid JSON.
This parameter should contain **only** query part and not include other ES search request components, such as aggregation or filtering.
* `<query-evaluation-mode>` - query evaluation mode. Can be `scheduled` or `ondemand`.
* `<query-type>` - query type. Can be `default` or `raw`. Different fields are required based on query type.

### Schema 
```html
# Query name. Should be unique.
name: <string>

# Query type
[ type: <query-type> | default: 'default']
  
# Elasic indexes to run query on
indices:
- <es-index-format>

# Explicit query timeout
[ timeout: <duration> | default: null ]

# Explicit query evaluation interval (ignored for on-demand queries)
[ interval: <duration> | default: null ]

# Explicie query evaluation mode
[ evaluation_mode: <query-evaluation-mode> | default: null ]

# Query labels
labels:
[ - <name>: <value> ... ]

# Sliding date window. Ignored for 'raw' queries
[ sliding_date: <sliding-date-config> ]

# Only for 'default' queries, ignored for raw queries
[ query: <es-json-query> | default: null ]
  
# Only for 'raw' queries, ignored for default queries
[ request: <es-json-request> | default: null ]
```

### `<sliding-date-config>`

Block allows to describe the sliding date window to process only certain records in a specified time interval. Window boundaries are defined as follows

`date.now > record.{field} >= (date.now - {range})`

```html
# Date field name to filter records
field: <string>

# Window range is ES format, e.g. 30m, 1d, 10s, etc
range: <es-time> 
```

## Query examples

### Default query
```yaml
name: "errors_query"
indices:
- 'app-logs-{0:yyyy.MM.dd}'

sliding_date:
  field: timestamp
  range: 30m

query: |
  {
      "bool": {
        "must": [
          {
            "match": {
              "level": "Error"
            }
          }
        ]
      }
  }
```

### Raw query
```yaml
name: "agent_query"
type: raw
evaluation_mode: ondemand

indices:
- 'app-logs-{0:yyyy.MM.dd}'

request: |
    {
      "size": 0,
      "query": {
        "bool": {
          "must": [
            {
              "match": {
                "agent": "Mozilla/5.0 (X11; Linux x86_64; rv:6.0a1) Gecko/20110421 Firefox/6.0a1"
              }
            },
            {
              "range": {
                "timestamp": {
                  "gte": "now-1d",
                  "lt": "now"
                }
              }
            }
          ]
        }
      },
      "aggs": {
        "avg_bytes" : { "avg" : { "field" : "bytes" } }
      }
    }
```
