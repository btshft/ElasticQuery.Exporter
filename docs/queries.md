# Queries

The metrics provided by the program are determined by queries. Queries can be evaluated on schedule or on demand. Query definition 
consists of raw JSON Elasticsearch query and additional metadata and parameters.

## Query definition

Queries are defined in YAML format. File schema described below.

Generic placeholders are defined as follows:
* `<es-index-format>` - elasticsearch index name. It can be specified as date-template, which will be rendered on query evaluation. For example `app-logs-{0:yyyy.MM.dd}`.
* `<es-json-query>` - elasticsearch query part. Should be valid JSON. 
This parameter should contain **only** query part and not include other ES search request components, such as aggregation or filtering.

### Schema 
```html
# Query name. Should be unique.
name: <string>

# Elasic indexes to run query on
indices:
- <es-index-format>

# Explicit query timeout
[ timeout: <duration> | default: null ]

# Explicit query evaluation interval (ignored for on-demand queries)
[ interval: <duration> | default: null ]

# Explicie query evaluation mode
[ evaluation_mode: <evaluation-mode> | default: null ]

# Query labels
labels:
[ - <name>: <value> ... ]

[ sliding_date: <sliding-date-config> ]

query: <es-json-query> 
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

## Query example
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