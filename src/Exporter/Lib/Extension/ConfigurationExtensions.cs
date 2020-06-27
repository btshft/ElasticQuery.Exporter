using System;
using Microsoft.Extensions.Configuration;

namespace ElasticQuery.Exporter.Lib.Extension
{
    public static class ConfigurationExtensions
    {
        public static TOptions CreateOptions<TOptions>(this IConfigurationSection configurationSection, Action<TOptions> validator) 
            where TOptions: class, new()
        {
            if (configurationSection == null) 
                throw new ArgumentNullException(nameof(configurationSection));

            if (!configurationSection.Exists())
                throw new InvalidOperationException("Section not exists");

            var options = new TOptions();
            configurationSection.Bind(options);

            if (validator != null)
                validator(options);

            return options;
        }
    }
}