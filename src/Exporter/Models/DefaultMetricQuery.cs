namespace ElasticQuery.Exporter.Models
{
    public class DefaultMetricQuery : MetricQuery
    {
        /// <inheritdoc />
        public override MetricQueryType Type => MetricQueryType.Default;

        public MetricQuerySlidingDate SlidingDate { get; set; }

        public string Query { get; set; }
    }
}