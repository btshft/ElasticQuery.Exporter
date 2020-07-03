using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace ElasticQuery.Exporter.Lib.File
{
    public class GlobFileProvider : IGlobFileProvider
    {
        private readonly string _basePath;

        public GlobFileProvider(string basePath)
        {
            if (!Directory.Exists(basePath))
                throw new ArgumentException($"Path '{basePath}' not found");

            _basePath = basePath;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetFiles(string globPattern)
        {
            var mather = new Matcher(StringComparison.InvariantCulture)
                .AddInclude(globPattern);

            var matches = mather.Execute(new DirectoryInfoWrapper(new DirectoryInfo(_basePath)));
            return matches.Files.Select(f => f.Path);
        }
    }
}