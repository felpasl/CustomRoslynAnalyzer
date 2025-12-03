## Roslyn Custom Analyzer Playground

This repository hosts a Roslyn analyzer package plus a small console application that references it so you can quickly try out custom rules without publishing a NuGet package first.

### Projects

- `CustomRulesAnalyzer` – Analyzer library that targets `netstandard2.0` and can be packed/published as a standalone analyzer package.
- `SampleApp` – .NET 9 console app wired up to the analyzer via a project reference (`OutputItemType="Analyzer"`), giving fast feedback while developing rules.

Both projects are included in `RoselynCustomAnalyzer.sln`.

### Implemented rules

| ID     | Category | Description |
|--------|----------|-------------|
| `CR0001` | Usage | Flags any call to `Console.WriteLine` to encourage the use of a logging abstraction. |
| `CR0002` | Naming | Requires public async methods to end with `Async` so their behavior is clear to callers. |

`SampleApp/Program.cs` intentionally violates both rules so you can see the diagnostics when running a build in the sample, Visual Studio, or VS Code.

### How to use

1. Restore/build everything:
   ```bash
   dotnet build
   ```
   The build output will show the analyzer warnings raised in `SampleApp`.
2. Run the sample console application (warnings still appear but the app runs):
   ```bash
   dotnet run --project SampleApp
   ```
3. Analyze `SampleApp` from the command line. The analyzer runs automatically during build, so invoking `dotnet build SampleApp` or the more thorough `dotnet build` from the repo root will surface diagnostics. You should see warnings `CR0001`/`CR0002` in the output—treat them as you would compiler warnings (they can fail CI if you turn `warningsAsErrors` on).
4. Reference the analyzer from another project by copying the `<ProjectReference ... OutputItemType="Analyzer" />` snippet from `SampleApp.csproj`, or pack it as a NuGet package:
   ```bash
   dotnet pack CustomRulesAnalyzer -c Release
   ```
   This command creates `CustomRulesAnalyzer/bin/Release/CustomRulesAnalyzer.1.0.0.nupkg`, which is just a zipped NuGet package. You can distribute that file directly or inspect it with any zip tool.

### Next steps

- Add more `DiagnosticAnalyzer` implementations in `CustomRulesAnalyzer`.
- Expand `AnalyzerReleases.Unshipped.md` / `.Shipped.md` as rules evolve so versioned release notes stay accurate.
- Add unit tests using `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing.XUnit` if you need automated verification.
