using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using ElasticQuery.Exporter.Models.Validators;
using FluentValidation;

namespace ElasticQuery.Exporter.Models.Mappers
{
    public static class MetricQueryMapper
    {
        private static IMapper Mapper { get; } = new Mapper(new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new MappingProfile());
        }));

        private static IEnumerable<IValidator> Validators { get; } = new IValidator[]
        {
            new RawMetricQueryValidator(),
            new DefaultMetricQueryValidator()
        };

        public static MetricQuery ToQuery(this MetricQueryDefinition definition)
        {
            return definition.Type == MetricQueryType.Raw
                ? (MetricQuery) Map<RawMetricQuery>(definition)
                : (MetricQuery) Map<DefaultMetricQuery>(definition);
        }

        private static TResult Map<TResult>(object source)
        {
            var result = Mapper.Map<TResult>(source);
            if (Validators.FirstOrDefault(v => v is AbstractValidator<TResult>) is AbstractValidator<TResult> validator)
                validator.ValidateAndThrow(result);

            return result;
        }

        private class MappingProfile : Profile
        {
            public MappingProfile()
            {
                CreateMap<MetricQueryDefinition, RawMetricQuery>();
                CreateMap<MetricQueryDefinition, DefaultMetricQuery>();
            }
        }
    }
}