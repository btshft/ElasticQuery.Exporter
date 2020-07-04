using System;
using FluentValidation;
using Newtonsoft.Json.Linq;

namespace ElasticQuery.Exporter.Models.Validators
{
    public class DefaultMetricQueryValidator : MetricQueryValidator<DefaultMetricQuery>
    {
        public DefaultMetricQueryValidator() 
        {
            RuleFor(s => s.Query)
                .NotNull();

            RuleFor(s => s.SlidingDate)
                .Custom((sliding, context) =>
                {
                    if (sliding == null)
                        return;

                    if (string.IsNullOrEmpty(sliding.Field))
                        context.AddFailure(nameof(sliding.Field), "field name is required");

                    if (string.IsNullOrWhiteSpace(sliding.Range))
                    {
                        context.AddFailure(nameof(sliding.Range), "date range is required");
                    }
                    else if (!MetricQuerySlidingDate.FieldRegex.IsMatch(sliding.Range))
                    {
                        context.AddFailure(nameof(sliding.Range), "range format is invalid, expected ES date format (e.g. 30m, 1h, 1d, etc)");
                    }
                });

            RuleFor(s => s)
                .Custom((query, context) =>
                {
                    try
                    {
                        var token = JObject.Parse(query.Query);
                        if (token == null)
                            context.AddFailure(nameof(query.Query), $"Query '{query.Name}' json query is invalid");
                    }
                    catch (Exception e)
                    {
                        context.AddFailure(nameof(query.Query), $"Query '{query.Name}' json query is invalid: {e.Message}");
                    }
                });
        }
    }
}