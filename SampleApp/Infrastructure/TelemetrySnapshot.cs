using System;

namespace SampleApp.Infrastructure;

public sealed class TelemetrySnapshot
{
    public TelemetrySnapshot(DateTimeOffset timestamp, string source, string payload)
    {
        Timestamp = timestamp;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        Payload = payload ?? string.Empty;
    }

    public DateTimeOffset Timestamp { get; }
    public string Source { get; }
    public string Payload { get; }

    public static TelemetrySnapshot Empty(Uri? source)
    {
        var fallback = source?.AbsoluteUri ?? "unknown";
        return new TelemetrySnapshot(DateTimeOffset.UtcNow, fallback, string.Empty);
    }
}
