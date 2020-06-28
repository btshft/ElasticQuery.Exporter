# Queries

The metrics provided by the program are determined by queries. Queries can be evaluated on schedule or on demand. Query definition contains of raw JSON Elasticsearch query and additional metadata and parameters.

## Query definition

Queries are defined in YAML format. File schema described below.

Generic placeholders are defined as follows:
* `<es_index_format>` - elasticsearch index name. It can be specified as date-template, which will be rendered on query evaluation. For example `app-logs-{0:yyyy.MM.dd}`.
* `<es_json_query>` - elasticsearch query part. Should be valid JSON. 
This parameter should contain **only** query part and not include other ES search request components, such as aggregation or filtering.

### Schema 
```html
# Query name. Should be unique.
name: <string>

# Elasic indexes to run query on
indices:
- <es_index_format>

# Explicit query timeout
[ timeout: <duration> | default: null ]

# Explicit query evaluation interval (ignored for on-demand queries)
[ interval: <duration> | default: null ]

# Explicie query evaluation mode
[ evaluation_mode: ondemand | default: null ]

# Query labels
labels:
[ - <name>: <value> ... ]

[ sliding_date: <sliding_date_config> ]

query: <es_json_query> 
```

### `<sliding_date_config>`

Block allows to describe the sliding date window to process only certain records in a specified time interval. Window boundaries are defined as follows

`date.now > record.{field} >= (date.now - {range})`

```html
# Date field name to filter records
field: <string>

# Window range is ES format, e.g. 30m, 1d, 10s, etc
range: <es_time> 
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