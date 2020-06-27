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