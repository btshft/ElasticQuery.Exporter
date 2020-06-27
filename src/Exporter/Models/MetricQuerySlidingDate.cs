using System.Text.RegularExpressions;

namespace ElasticQuery.Exporter.Models
{
    public class MetricQuerySlidingDate
    {
        public static readonly Regex FieldRegex = new Regex(
            @"^(?![0])(?<expression>[\d]+(?:y|M|w|d|h|m|s))$", RegexOptions.Compiled);

        /// <summary>
        /// Time in ES format, e.g.
        /// 10m, 5d, 15h, etc.
        /// </summary>
        public string Range { get; set; } = "30m";

        public string Field { get; set; } = "@timestamp";
    }
}