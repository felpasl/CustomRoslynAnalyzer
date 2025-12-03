using System.Threading;

namespace SampleApp.Infrastructure;

internal interface ITelemetryApiClient
{
    Task<TelemetrySnapshot> GetLatestTelemetryAsync(CancellationToken cancellationToken = default);
}
