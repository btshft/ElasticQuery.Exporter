namespace ElasticQuery.Exporter.Services.QueryExecutor.Results
{
    public class ValueAggregationResult
    {
        public ValueAggregationResult(string key, double? value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public double? Value { get; }
    }
}