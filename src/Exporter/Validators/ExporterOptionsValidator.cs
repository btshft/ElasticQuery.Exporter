using System;
using System.IO;
using ElasticQuery.Exporter.Options;
using FluentValidation;

namespace ElasticQuery.Exporter.Validators
{
    public class ExporterOptionsValidator : AbstractValidator<ExporterOptions>
    {
        public ExporterOptionsValidator()
        {
            RuleFor(e => e.Metrics)
                .NotNull();

            RuleFor(e => e.ElasticSearch)
                .NotNull();

            RuleFor(e => e.ElasticSearch.Connection)
                .NotNull()
                .Custom((options, context) =>
                {
                    if (options?.SingleNode == null && options?.StaticCluster == null)
                        context.AddFailure("Connection options not specified");
                });

            RuleFor(e => e.QueryFiles)
                .ForEach(collection =>
                {
                    collection.Custom((filePath, context) =>
                    {
                        if (string.IsNullOrWhiteSpace(filePath))
                        {
                            context.AddFailure("Query file path cannot be null");
                            return;
                        }

                        if (!File.Exists(filePath))
                            context.AddFailure($"Query file '{filePath}' not exists");
                    });
                });

            RuleFor(e => e.Metrics.Evaluation)
                .NotNull();

            RuleFor(e => e.Metrics.Evaluation)
                .Custom((evaluation, context) =>
                {
                    var source = (ExporterOptions) context.InstanceToValidate;

                    if (evaluation != null)
                    {
                        if (evaluation.Timeout > source.Metrics.Evaluation.Interval)
                            context.AddFailure("Timeout cannot be greater than evaluation interval");

                        if (evaluation.Timeout <= TimeSpan.Zero)
                            context.AddFailure("Timeout should be more than 0");

                        if (evaluation.Interval <= TimeSpan.Zero)
                            context.AddFailure("Interval should be more than 0");
                    }
                });
        }
    }
}
