using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CustomRulesAnalyzer.Rules;

internal interface IAnalyzerRule
{
    DiagnosticDescriptor Descriptor { get; }
    void Register(CompilationStartAnalysisContext context);
}
