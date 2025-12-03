using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.Infrastructure;

internal sealed class InMemoryTelemetryRepository : ITelemetryRepository
{
    private readonly List<TelemetrySnapshot> _snapshots = new();
    private readonly object _sync = new();

    public Task SaveAsync(TelemetrySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            _snapshots.Add(snapshot);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<TelemetrySnapshot> GetAll()
    {
        lock (_sync)
        {
            return _snapshots.ToArray();
        }
    }
}
