using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace ElasticQuery.Exporter.Lib.Serilog.Enrichers
{
    public class DemistifyExceptionEnricher : ILogEventEnricher
    {
        /// <inheritdoc />
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent.Exception != null)
                logEvent.Exception.Demystify();
        }
    }
}
