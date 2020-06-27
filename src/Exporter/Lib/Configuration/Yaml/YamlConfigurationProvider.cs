using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using YamlDotNet.RepresentationModel;

namespace ElasticQuery.Exporter.Lib.Configuration.Yaml
{
    public class YamlConfigurationProvider : FileConfigurationProvider
    {
        /// <inheritdoc />
        public YamlConfigurationProvider(FileConfigurationSource source) 
            : base(source)
        {
        }

        /// <inheritdoc />
        public override void Load(Stream stream)
        {
            try
            {
                Data = Parse(stream);
            }
            catch (Exception e)
            {
                throw new FormatException("Unable to parse yaml configuration", e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IDictionary<string, string> Parse(Stream stream)
        {
            var yaml = new YamlStream();
            yaml.Load(new StreamReader(stream, encoding: Encoding.UTF8));

            if (yaml.Documents.Any())
            {
                var document = yaml.Documents.First();
                var visitor = new PropertiesVisitor();

                visitor.Visit(document);

                return visitor.Properties;
            }

            return new Dictionary<string, string>();
        }

        private class PropertiesVisitor : YamlVisitor
        {
            public IDictionary<string, string> Properties { get; } = new SortedDictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            /// <inheritdoc />
            protected override void VisitScalar(YamlScalarNode scalar)
            {
                var currentKey = Path
                    .Replace("_", "")
                    .Replace("-", "");

                if (Properties.ContainsKey(currentKey))
                    throw new FormatException($"Key '{currentKey}' already exists");

                Properties[currentKey] = IsNull(scalar) ? null : scalar.Value;
            }

            private static bool IsNull(YamlScalarNode yamlValue)
            {
                return yamlValue.Style == YamlDotNet.Core.ScalarStyle.Plain
                       && (
                           yamlValue.Value == "~"
                           || yamlValue.Value == "null"
                           || yamlValue.Value == "Null"
                           || yamlValue.Value == "NULL"
                       );
            }
        }
    }
}
