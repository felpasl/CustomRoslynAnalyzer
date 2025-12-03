using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CustomRulesAnalyzer.Rules.Configuration;

namespace CustomRulesAnalyzer.Rules;

internal sealed class AvoidConsoleWriteLineRule : IAnalyzerRule
{
    private static readonly RuleDescriptorInfo Info = new(
        id: "CR0001",
        title: "Avoid Console.WriteLine",
        messageFormat: "Use a logging abstraction instead of Console.WriteLine",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        enabledByDefault: true,
        description: "Console.WriteLine makes automated testing harder. Use ILogger or another abstraction instead.");

    public static DiagnosticDescriptor DefaultDescriptor =>
        RuleDescriptorFactory.Create(Info, RuleConfiguration.FromDefaults(Info));

    public DiagnosticDescriptor Descriptor { get; }

    private readonly bool _isEnabled;

    public AvoidConsoleWriteLineRule(IRuleConfigurationSource configurationSource)
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

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation ||
            invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;
        if (symbol is null)
        {
            return;
        }

        if (symbol.ContainingType?.ToDisplayString() == "System.Console" &&
            symbol.Name == "WriteLine")
        {
            context.ReportDiagnostic(Diagnostic.Create(Descriptor, memberAccess.Name.GetLocation()));
        }
    }
}
