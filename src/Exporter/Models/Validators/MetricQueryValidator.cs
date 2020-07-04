using System;
using FluentValidation;

namespace ElasticQuery.Exporter.Models.Validators
{
    public abstract class MetricQueryValidator<TQuery> : AbstractValidator<TQuery> 
        where TQuery : MetricQuery
    {
        protected MetricQueryValidator()
        {
            RuleFor(s => s)
                .NotNull();

            RuleFor(s => s.Name)
                .NotEmpty();

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
        }
    }
}