using Microsoft.Extensions.Configuration;

namespace ElasticQuery.Exporter.Lib.Configuration.Yaml
{
    public class YamlConfigurationSource : FileConfigurationSource
    {
        /// <inheritdoc />
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new YamlConfigurationProvider(this);
        }
    }
}