using System.Collections.Generic;

namespace ElasticQuery.Exporter.Lib.File
{
    public interface IGlobFileProvider
    {
        IEnumerable<string> GetFiles(string globPattern);
    }
}
