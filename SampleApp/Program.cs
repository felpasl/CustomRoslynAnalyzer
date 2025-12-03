using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using SampleApp.Infrastructure;

namespace SampleApp;

internal static class Program
{
    internal static async Task Main(string[] args)
    {
        var count = args.Length > 0 && int.TryParse(args[0], out var parsedCount) ? parsedCount : 10;
        await DemoApp.RunAsync(count);
    }
}

internal static class DemoApp
{
    private static readonly Uri TelemetryEndpoint = new("https://example.com/api/telemetry");

    public static async Task RunAsync(int count)
    {
        Trace.WriteLine("Bootstrapping the demo...");

        using var httpClient = new HttpClient();
        var apiClient = new TelemetryApiClient(httpClient, TelemetryEndpoint);
        var repository = new InMemoryTelemetryRepository();
        var worker = new BackgroundWorker(apiClient, repository);

        for (var i = 0; i < count; i++)
        {
            Trace.WriteLine($"Iteration {i + 1}/{count}");
            await worker.FetchTelemetryAsync();
            _ = await worker.ComputeAsync();
        }
        
        foreach (var entry in repository.GetAll())
        {
            Trace.WriteLine($"[{entry.Timestamp:O}] {entry.Source} -> {entry.Payload.Length} chars");
        }
    }
}

internal sealed class BackgroundWorker
{
    private readonly ITelemetryApiClient _apiClient;
    private readonly ITelemetryRepository _repository;

    public BackgroundWorker(ITelemetryApiClient apiClient, ITelemetryRepository repository)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task FetchTelemetryAsync()
    {
        var snapshot = await _apiClient.GetLatestTelemetryAsync();
        await _repository.SaveAsync(snapshot);
        Trace.WriteLine($"Telemetry fetched from {snapshot.Source}.");
    }

    public async Task<int> ComputeAsync()
    {
        await Task.Delay(50);
        return Random.Shared.Next(0, 100);
    }
}
