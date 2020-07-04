using System;
using FluentValidation;
using Newtonsoft.Json.Linq;

namespace ElasticQuery.Exporter.Models.Validators
{
    public class RawMetricQueryValidator : MetricQueryValidator<RawMetricQuery>
    {
        public RawMetricQueryValidator()
        {
            RuleFor(s => s)
                .Custom((query, context) =>
                {
                    try
                    {
                        var token = JObject.Parse(query.Request);
                        if (token == null)
                            context.AddFailure(nameof(RawMetricQuery.Request), $"Query '{query.Name}' json request is invalid");
                    }
                    catch (Exception e)
                    {
                        context.AddFailure(nameof(RawMetricQuery.Request), $"Query '{query.Name}' json request is invalid: {e.Message}");
                    }
                });
        }
    }
}