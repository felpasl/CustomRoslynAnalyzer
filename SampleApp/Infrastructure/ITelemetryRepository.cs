using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.Infrastructure;

internal interface ITelemetryRepository
{
    Task SaveAsync(TelemetrySnapshot snapshot, CancellationToken cancellationToken = default);
    IReadOnlyCollection<TelemetrySnapshot> GetAll();
}
