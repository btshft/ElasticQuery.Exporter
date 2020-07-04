using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ElasticQuery.Exporter.HealthCheck
{
    public class MetricsSchedulerInitializedHealthCheck : IHealthCheck
    {
        private volatile bool _schedulerInitialized;

        public bool SchedulerInitialized
        {
            get => _schedulerInitialized;
            set => _schedulerInitialized = value;
        }

        /// <inheritdoc />
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SchedulerInitialized 
                ? HealthCheckResult.Healthy("Scheduler initialization finished.") 
                : HealthCheckResult.Unhealthy("Scheduler initialization  is still running."));
        }
    }
}