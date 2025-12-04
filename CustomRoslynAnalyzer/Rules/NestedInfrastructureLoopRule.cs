using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CustomRoslynAnalyzer.Configuration;
using CustomRoslynAnalyzer.Core;

namespace CustomRoslynAnalyzer.Rules;

public sealed class NestedInfrastructureLoopRule : IAnalyzerRule
{
    private const string DiagnosticId = "CR0003";
    private const string Title = "Avoid infrastructure calls inside loops";
    private const string MessageFormat = "Avoid invoking infrastructure-layer types from loop bodies";
    private const string Category = "Usage";
    private const string Description =
        "Infrastructure operations may be costly; keep them outside loop iterations.";

    private static readonly RuleDescriptorInfo Info = new(
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        enabledByDefault: true,
        description: Description);

    private static readonly DiagnosticDescriptor DefaultRuleDescriptor = new(
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    private static readonly SyntaxKind[] LoopKinds =
    {
        SyntaxKind.ForStatement,
        SyntaxKind.ForEachStatement,
        SyntaxKind.ForEachVariableStatement,
        SyntaxKind.WhileStatement,
        SyntaxKind.DoStatement
    };

    public static DiagnosticDescriptor DefaultDescriptor => DefaultRuleDescriptor;

    public DiagnosticDescriptor Descriptor { get; }

    private readonly bool _isEnabled;
    private readonly ConcurrentDictionary<ISymbol, bool> _methodInfrastructureCache =
        new(SymbolEqualityComparer.Default);

    private readonly RuleConfiguration _configuration;

    public NestedInfrastructureLoopRule(IRuleConfigurationSource configurationSource)
    {
        _configuration = configurationSource.GetConfiguration(Info);
        Descriptor = RuleDescriptorFactory.Create(Info, _configuration);
        _isEnabled = _configuration.IsEnabled;
    }

    public void Register(CompilationStartAnalysisContext context)
    {
        if (!_isEnabled || _configuration.Severity == DiagnosticSeverity.Hidden)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitCreation, SyntaxKind.ImplicitObjectCreationExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!IsInsideLoop(invocation))
        {
            return;
        }

        if (!TryGetInfrastructureSymbol(context.SemanticModel.GetSymbolInfo(invocation), out var infrastructureSymbol))
        {
            infrastructureSymbol = FindInfrastructureMethod(context.SemanticModel.GetSymbolInfo(invocation), context.SemanticModel.Compilation);
            if (infrastructureSymbol is null)
            {
                return;
            }
        }

#if DEBUG
        var symbolDescription = infrastructureSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        Debug.WriteLine($"[CR0003] Invocation '{invocation}' of infrastructure '{symbolDescription}' is inside a loop.");
#endif

        var location = invocation.GetLocation();
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            location = memberAccess.Name.GetLocation();
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptor, location));
    }

    private void AnalyzeCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        if (!IsInsideLoop(creation))
        {
            return;
        }

        var creationType = context.SemanticModel.GetSymbolInfo(creation.Type).Symbol as ITypeSymbol
                            ?? context.SemanticModel.GetTypeInfo(creation).Type;
        if (!IsInfrastructureSymbol(creationType))
        {
            return;
        }

#if DEBUG
        var symbolDescription = creationType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        Debug.WriteLine($"[CR0003] Creation '{creation}' of infrastructure '{symbolDescription}' is inside a loop.");
#endif

        context.ReportDiagnostic(Diagnostic.Create(Descriptor, creation.GetLocation()));
    }

    private void AnalyzeImplicitCreation(SyntaxNodeAnalysisContext context)
    {
        var implicitCreation = (ImplicitObjectCreationExpressionSyntax)context.Node;
        if (!IsInsideLoop(implicitCreation))
        {
            return;
        }

        var implicitType = context.SemanticModel.GetTypeInfo(implicitCreation).Type;
        if (!IsInfrastructureSymbol(implicitType))
        {
            return;
        }

#if DEBUG
        var symbolDescription = implicitType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        Debug.WriteLine($"[CR0003] Implicit creation '{implicitCreation}' of infrastructure '{symbolDescription}' is inside a loop.");
#endif

        context.ReportDiagnostic(Diagnostic.Create(Descriptor, implicitCreation.GetLocation()));
    }

    private static bool IsInsideLoop(SyntaxNode node)
    {
        for (var current = node.Parent; current is not null; current = current.Parent)
        {
            if (LoopKinds.Contains(current.Kind()))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindInfrastructureUsage(
        SyntaxNode loopBody,
        SemanticModel semanticModel,
        out SyntaxNode? offendingNode,
        out ISymbol? infrastructureSymbol)
    {
        foreach (var node in loopBody.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case InvocationExpressionSyntax invocation:
                    var invocationSymbol = semanticModel.GetSymbolInfo(invocation);
                    if (TryGetInfrastructureSymbol(invocationSymbol, out infrastructureSymbol))
                    {
                        offendingNode = invocation;
                        return true;
                    }

                    var indirectSymbol = FindInfrastructureMethod(invocationSymbol, semanticModel.Compilation);
                    if (indirectSymbol is not null)
                    {
                        offendingNode = invocation;
                        infrastructureSymbol = indirectSymbol;
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

    private IMethodSymbol? FindInfrastructureMethod(SymbolInfo symbolInfo, Compilation compilation)
    {
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
            CallsMethodWithInfrastructure(methodSymbol, compilation))
        {
            return methodSymbol;
        }

        if (symbolInfo.CandidateReason == CandidateReason.None)
        {
            return null;
        }

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (candidate is IMethodSymbol candidateMethod &&
                CallsMethodWithInfrastructure(candidateMethod, compilation))
            {
                return candidateMethod;
            }
        }

        return null;
    }

    private bool CallsMethodWithInfrastructure(IMethodSymbol methodSymbol, Compilation compilation)
    {
        return _methodInfrastructureCache.GetOrAdd(methodSymbol, symbol =>
        {
            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxReference.GetSyntax();
                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
                var body = ExtractBodyNode(syntax);
                if (body is null)
                {
                    continue;
                }

                if (ContainsInfrastructureUsage(body, semanticModel))
                {
                    return true;
                }
            }

            return false;
        });
    }

    private static SyntaxNode? ExtractBodyNode(SyntaxNode syntax) => syntax switch
    {
        BaseMethodDeclarationSyntax methodSyntax =>
            (SyntaxNode?)methodSyntax.Body ?? methodSyntax.ExpressionBody?.Expression,
        AccessorDeclarationSyntax accessorSyntax =>
            (SyntaxNode?)accessorSyntax.Body ?? accessorSyntax.ExpressionBody?.Expression,
        LocalFunctionStatementSyntax localFunction =>
            (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody?.Expression,
        _ => null
    };

    private static StatementSyntax? GetLoopBody(SyntaxNode loop) => loop switch
    {
        ForStatementSyntax forStatement => forStatement.Statement,
        ForEachStatementSyntax foreachStatement => foreachStatement.Statement,
        ForEachVariableStatementSyntax foreachVar => foreachVar.Statement,
        WhileStatementSyntax whileStatement => whileStatement.Statement,
        DoStatementSyntax doStatement => doStatement.Statement,
        _ => null
    };

    private static bool ContainsInfrastructureUsage(SyntaxNode node, SemanticModel semanticModel)
    {
        foreach (var descendant in node.DescendantNodesAndSelf())
        {
            if (descendant is ExpressionSyntax expression)
            {
                var typeInfo = semanticModel.GetTypeInfo(expression);
                if (IsInfrastructureSymbol(typeInfo.Type) || IsInfrastructureSymbol(typeInfo.ConvertedType))
                {
                    return true;
                }
            }

            var symbol = semanticModel.GetSymbolInfo(descendant).Symbol;
            if (IsInfrastructureSymbol(symbol))
            {
                return true;
            }
        }

        return false;
    }

    private const string InfrastructureNamespaceToken = "Infrastructure";

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

        return ns.IndexOf(InfrastructureNamespaceToken, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
