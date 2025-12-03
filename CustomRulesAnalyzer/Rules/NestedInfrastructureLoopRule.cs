using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CustomRulesAnalyzer.Rules.Configuration;

namespace CustomRulesAnalyzer.Rules;

internal sealed class NestedInfrastructureLoopRule : IAnalyzerRule
{
    private static readonly RuleDescriptorInfo Info = new(
        id: "CR0003",
        title: "Avoid infrastructure calls inside loops",
        messageFormat: "Avoid invoking infrastructure-layer types from loop bodies",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        enabledByDefault: true,
        description: "Infrastructure operations may be costly; keep them outside loop iterations.");

    private static readonly SyntaxKind[] LoopKinds =
    {
        SyntaxKind.ForStatement,
        SyntaxKind.ForEachStatement,
        SyntaxKind.ForEachVariableStatement,
        SyntaxKind.WhileStatement,
        SyntaxKind.DoStatement
    };

    public static DiagnosticDescriptor DefaultDescriptor =>
        RuleDescriptorFactory.Create(Info, RuleConfiguration.FromDefaults(Info));

    public DiagnosticDescriptor Descriptor { get; }

    private readonly bool _isEnabled;

    public NestedInfrastructureLoopRule(IRuleConfigurationSource configurationSource)
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

        context.RegisterSyntaxNodeAction(AnalyzeLoop, LoopKinds);
    }

    private void AnalyzeLoop(SyntaxNodeAnalysisContext context)
    {
        if (!TryFindInfrastructureUsage(context.Node, context.SemanticModel, out var offendingNode, out var symbol))
        {
            return;
        }

#if DEBUG
        var symbolDescription = symbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            ?? offendingNode?.ToString()
            ?? "<unknown>";
        Debug.WriteLine($"[CR0003] Loop '{context.Node.Kind()}' contains infrastructure usage '{symbolDescription}'.");
#endif

        var location = offendingNode?.GetLocation() ?? context.Node.GetLocation();
        context.ReportDiagnostic(Diagnostic.Create(Descriptor, location));
    }

    private static bool TryFindInfrastructureUsage(
        SyntaxNode loopNode,
        SemanticModel semanticModel,
        out SyntaxNode? offendingNode,
        out ISymbol? infrastructureSymbol)
    {
        foreach (var node in loopNode.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation:
                    if (TryGetInfrastructureSymbol(semanticModel.GetSymbolInfo(invocation), out infrastructureSymbol))
                    {
                        offendingNode = invocation;
                        return true;
                    }

                    break;

                case ObjectCreationExpressionSyntax creation:
                    var creationType = semanticModel.GetSymbolInfo(creation.Type).Symbol as ITypeSymbol
                                        ?? semanticModel.GetTypeInfo(creation).Type;
                    if (IsInfrastructureSymbol(creationType))
                    {
                        offendingNode = creation;
                        infrastructureSymbol = creationType;
                        return true;
                    }

                    break;

                case ImplicitObjectCreationExpressionSyntax implicitCreation:
                    var implicitType = semanticModel.GetTypeInfo(implicitCreation).Type;
                    if (IsInfrastructureSymbol(implicitType))
                    {
                        offendingNode = implicitCreation;
                        infrastructureSymbol = implicitType;
                        return true;
                    }

                    break;
            }
        }

        offendingNode = null;
        infrastructureSymbol = null;
        return false;
    }

    private static bool TryGetInfrastructureSymbol(SymbolInfo symbolInfo, out ISymbol? infrastructureSymbol)
    {
        if (IsInfrastructureSymbol(symbolInfo.Symbol))
        {
            infrastructureSymbol = symbolInfo.Symbol;
            return true;
        }

        if (symbolInfo.CandidateReason == CandidateReason.None)
        {
            infrastructureSymbol = null;
            return false;
        }

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (IsInfrastructureSymbol(candidate))
            {
                infrastructureSymbol = candidate;
                return true;
            }
        }

        infrastructureSymbol = null;
        return false;
    }

    private const string InfrastructureNamespace = "SampleApp.Infrastructure";

    private static bool IsInfrastructureSymbol(ISymbol? symbol)
    {
        if (symbol is null)
        {
            return false;
        }

        switch (symbol)
        {
            case ITypeSymbol typeSymbol:
                return IsInfrastructureType(typeSymbol);
            case IMethodSymbol methodSymbol:
                return IsInfrastructureType(methodSymbol.ContainingType);
            case IPropertySymbol propertySymbol:
                return IsInfrastructureType(propertySymbol.ContainingType);
            case IFieldSymbol fieldSymbol:
                return IsInfrastructureType(fieldSymbol.ContainingType);
            case IEventSymbol eventSymbol:
                return IsInfrastructureType(eventSymbol.ContainingType);
            case IParameterSymbol parameterSymbol:
                return IsInfrastructureType(parameterSymbol.Type);
            case ILocalSymbol localSymbol:
                return IsInfrastructureType(localSymbol.Type);
            case IAliasSymbol aliasSymbol when aliasSymbol.Target is ITypeSymbol aliasType:
                return IsInfrastructureType(aliasType);
        }

        return IsInfrastructureNamespace(symbol.ContainingNamespace);
    }

    private static bool IsInfrastructureType(ITypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
        {
            return false;
        }

        return IsInfrastructureNamespace(typeSymbol.ContainingNamespace);
    }

    private static bool IsInfrastructureNamespace(INamespaceSymbol? namespaceSymbol)
    {
        var ns = namespaceSymbol?.ToDisplayString();
        if (ns is null)
        {
            return false;
        }

        return ns.StartsWith(InfrastructureNamespace, StringComparison.Ordinal);
    }
}
