using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using CustomRulesAnalyzer.Rules;
using CustomRulesAnalyzer.Rules.Configuration;

namespace CustomRulesAnalyzer;

/// <summary>
/// Custom analyzer that demonstrates two rules:
/// 1) Disallow Console.WriteLine usage.
/// 2) Ensure async methods are suffixed with Async.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CustomUsageAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            AvoidConsoleWriteLineRule.DefaultDescriptor,
            PublicAsyncSuffixRule.DefaultDescriptor,
            NestedInfrastructureLoopRule.DefaultDescriptor);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var configurationSource =
                RuleConfigurationSourceFactory.Create(compilationContext.Options.AnalyzerConfigOptionsProvider);
            var rules = new IAnalyzerRule[]
            {
                new AvoidConsoleWriteLineRule(configurationSource),
                new PublicAsyncSuffixRule(configurationSource),
                new NestedInfrastructureLoopRule(configurationSource)
            };

            foreach (var rule in rules)
            {
                rule.Register(compilationContext);
            }
        });
    }
}
