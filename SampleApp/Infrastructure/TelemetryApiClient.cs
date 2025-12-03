using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApp.Infrastructure;

internal sealed class TelemetryApiClient : ITelemetryApiClient
{
    private static readonly Uri DefaultEndpoint = new("https://example.com/api/telemetry");

    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;

    public TelemetryApiClient(HttpClient httpClient, Uri? endpoint = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint ?? DefaultEndpoint;
    }

    public async Task<TelemetrySnapshot> GetLatestTelemetryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _endpoint);
            request.Headers.Accept.ParseAdd("application/json");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            return new TelemetrySnapshot(DateTimeOffset.UtcNow, _endpoint.AbsoluteUri, payload);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Failed to retrieve telemetry: {ex.Message}");
            return TelemetrySnapshot.Empty(_endpoint);
        }
    }
}
