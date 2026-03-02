using System.Collections.Concurrent;

namespace Vara.Api.Services.Monitoring;

public record JobHealthStatus(
    DateTime LastRunAt,
    string Status,          // "success" | "failed" | "running"
    string? LastError,
    int ConsecutiveFailures);

public class BackgroundJobHealthMonitor
{
    private readonly ConcurrentDictionary<string, JobHealthStatus> _jobs = new();

    public void RecordStart(string jobName)
    {
        _jobs.AddOrUpdate(jobName,
            _ => new JobHealthStatus(DateTime.UtcNow, "running", null, 0),
            (_, prev) => prev with { Status = "running", LastRunAt = DateTime.UtcNow });
    }

    public void RecordSuccess(string jobName)
    {
        _jobs.AddOrUpdate(jobName,
            _ => new JobHealthStatus(DateTime.UtcNow, "success", null, 0),
            (_, prev) => new JobHealthStatus(DateTime.UtcNow, "success", null, 0));
    }

    public void RecordFailure(string jobName, string error)
    {
        _jobs.AddOrUpdate(jobName,
            _ => new JobHealthStatus(DateTime.UtcNow, "failed", error, 1),
            (_, prev) => new JobHealthStatus(
                DateTime.UtcNow, "failed", error,
                prev.ConsecutiveFailures + 1));
    }

    public IReadOnlyDictionary<string, JobHealthStatus> GetAll() => _jobs;
}
