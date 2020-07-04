namespace ElasticQuery.Exporter.Models
{
    public class RawMetricQuery : MetricQuery
    {
        /// <inheritdoc />
        public override MetricQueryType Type => MetricQueryType.Raw;

        public string Request { get; set; }
    }
}