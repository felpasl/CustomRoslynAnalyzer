## Roslyn Custom Analyzer package

`CustomRoslynAnalyzer` is a .NET Standard 2.0 analyzer library that ships as a NuGet package (the workflow already packs/publishes it) and exposes the following diagnostics to consumers:

| ID | Category | Description |
|----|----------|-------------|
| `CR0001` | Usage | Flags `Console.WriteLine` calls so you can enforce a logging abstraction instead of writing directly to standard output. |
| `CR0002` | Naming | Requires public async methods to end with `Async`, making their asynchronous nature explicit. |
| `CR0003` | Usage | Warns whenever a loop body invokes services deeper in `Infrastructure` namespaces, helping avoid repeated expensive work. |

Since these rules run inside the analyzer, any consuming project that references the NuGet package (or project reference with `OutputItemType="Analyzer"`) will surface `CR0001`/`CR0002`/`CR0003` as compiler warnings.

### Violations in `SampleApp`

`SampleApp/Program.cs` intentionally demonstrates the diagnostics emitted by the analyzer. In the loop below, the analyzer flags the `Console.WriteLine` call (`CR0001`) and the repetitive call to a service defined in `SampleApp.Infrastructure` (`CR0003`):

```csharp
for (var i = 0; i < count; i++)
{
	Trace.WriteLine($"Iteration {i + 1}/{count}");
	await worker.FetchTelemetryAsync(); // violates CR0003 because `TelemetryApiClient` lives under SampleApp.Infrastructure
	_ = await worker.Compute(); // violates CR0002 because Comupte() do not end with `Async` 
}

Console.WriteLine($"Telemetry fetched from {snapshot.Source}."); // violates CR0001
```

The analyzer also warns if any public async methods (such as `BackgroundWorker.Compute`) do not end with `Async`, which is how `CR0002` stays enforced.

## Next steps

- Add more `DiagnosticAnalyzer` implementations (and accompanying code fixes, if desired) under `Rules/`.
- Keep `AnalyzerReleases.Shipped.md` and `.Unshipped.md` in sync with released rule IDs so versioned release notes stay accurate.
- Add analyzer unit tests using `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit` when you need automated verification of diagnostics.
