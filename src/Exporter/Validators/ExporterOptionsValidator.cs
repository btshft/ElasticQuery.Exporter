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
                    if (options?.SingleNode == null)
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

            RuleFor(e => e.Metrics.Evaluation.Timeout)
                .Custom((timeout, context) =>
                {
                    var source = (ExporterOptions) context.InstanceToValidate;
                    if (source.Metrics?.Evaluation != null && timeout > source.Metrics.Evaluation.Interval)
                        context.AddFailure("Timeout cannot be greater than evaluation period");
                });
        }
    }
}
