using System;
using ElasticQuery.Exporter.Models;
using FluentValidation;
using Newtonsoft.Json.Linq;

namespace ElasticQuery.Exporter.Validators
{
    public class MetricQueryValidator : AbstractValidator<MetricQuery>
    {
        public MetricQueryValidator()
        {
            RuleFor(s => s)
                .NotNull();

            RuleFor(s => s.Name)
                .NotEmpty();

            RuleFor(s => s.Query)
                .NotNull();

            RuleFor(s => s)
                .Custom((query, context) =>
                {
                    if (query.Interval.HasValue && query.Timeout.HasValue)
                    {
                        if (query.Timeout.Value > query.Interval.Value)
                            context.AddFailure(nameof(query.Timeout), "Timeout cannot be greater than evaluation interval");
                    }

                    if (query.Interval.HasValue && query.Interval.Value <= TimeSpan.Zero)
                        context.AddFailure("Interval should be more than 0");

                    if (query.Timeout.HasValue && query.Timeout.Value <= TimeSpan.Zero)
                        context.AddFailure("Timeout should be more than 0");
                });

            RuleFor(s => s.SlidingDate)
                .Custom((sliding, context) =>
                {
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
                        if (query.Query == null)
                            return;

                        var token = JObject.Parse(query.Query);
                        if (token == null)
                            context.AddFailure(nameof(query.Query), $"Query '{query.Name}' json-query is invalid");
                    }
                    catch (Exception e)
                    {
                        context.AddFailure(nameof(query.Query), $"Query '{query.Name}' json-query is invalid: {e.Message}");
                    }
                });
        }
    }
}