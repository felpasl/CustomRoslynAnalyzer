using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using CustomRulesAnalyzer.Rules.Configuration;

namespace CustomRulesAnalyzer.Rules;

internal sealed class PublicAsyncSuffixRule : IAnalyzerRule
{
    private static readonly RuleDescriptorInfo Info = new(
        id: "CR0002",
        title: "Async method names should end with Async",
        messageFormat: "Rename '{0}' to end with Async to clarify asynchronous usage",
        category: "Naming",
        defaultSeverity: DiagnosticSeverity.Warning,
        enabledByDefault: true,
        description: "Async methods should end with Async so consumers understand they run asynchronously.");

    public static DiagnosticDescriptor DefaultDescriptor =>
        RuleDescriptorFactory.Create(Info, RuleConfiguration.FromDefaults(Info));

    public DiagnosticDescriptor Descriptor { get; }

    private readonly bool _isEnabled;

    public PublicAsyncSuffixRule(IRuleConfigurationSource configurationSource)
    {
        var configuration = configurationSource.GetConfiguration(Info);
        Descriptor = RuleDescriptorFactory.Create(Info, configuration);
        _isEnabled = configuration.IsEnabled;
    }

    public void Register(CompilationStartAnalysisContext context)
    {
        if (!_isEnabled)
        {
            return;
        }

        context.RegisterSymbolAction(AnalyzeMethodSymbol, SymbolKind.Method);
    }

    private void AnalyzeMethodSymbol(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!methodSymbol.IsAsync &&
            !ReturnsTask(methodSymbol))
        {
            return;
        }

        if (methodSymbol.MethodKind is
            MethodKind.PropertyGet or
            MethodKind.PropertySet or
            MethodKind.EventAdd or
            MethodKind.EventRemove or
            MethodKind.EventRaise or
            MethodKind.Destructor or
            MethodKind.StaticConstructor or
            MethodKind.Constructor)
        {
            return;
        }

        if (methodSymbol.Name.EndsWith("Async", StringComparison.Ordinal))
        {
            return;
        }

        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return;
        }

        var location = methodSymbol.Locations.FirstOrDefault();
        if (location != null)
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, location, methodSymbol.Name));
        }
    }

    private static bool ReturnsTask(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        return returnType.Name is "Task" or "ValueTask";
    }
}
