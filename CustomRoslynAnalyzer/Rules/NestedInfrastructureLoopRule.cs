// Copyright 2025 felpasl
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace CustomRoslynAnalyzer.Rules;
using CustomRoslynAnalyzer.Configuration;
using CustomRoslynAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

/// <summary>
/// Analyzer rule that warns when infrastructure-layer services are called from within loop bodies.
/// </summary>
public sealed class NestedInfrastructureLoopRule : IAnalyzerRule
{
    private const string DiagnosticId = "CR0003";
    private const string Title = "Avoid infrastructure calls inside loops";
    private const string MessageFormat = "Avoid invoking infrastructure-layer types from loop bodies";
    private const string Category = "Usage";
    private const string Description =
        "Infrastructure operations may be costly; keep them outside loop iterations.";

    private const string InfrastructureNamespaceToken = "Infrastructure";

    private static readonly RuleDescriptorInfo Info = new (
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        enabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/felpasl/CustomRoselynAnalyzer/blob/main/doc/CR0003.md");

    private static readonly DiagnosticDescriptor DefaultRuleDescriptor = new (
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/felpasl/CustomRoselynAnalyzer/blob/main/doc/CR0003.md");

    private static readonly SyntaxKind[] LoopKinds =
    {
        SyntaxKind.ForStatement,
        SyntaxKind.ForEachStatement,
        SyntaxKind.ForEachVariableStatement,
        SyntaxKind.WhileStatement,
        SyntaxKind.DoStatement,
    };

    private readonly bool isEnabled;

    private readonly ConcurrentDictionary<ISymbol, bool> methodInfrastructureCache =
        new (SymbolEqualityComparer.Default);

    private readonly RuleConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="NestedInfrastructureLoopRule"/> class.
    /// </summary>
    /// <param name="configurationSource">The source for rule configuration.</param>
    public NestedInfrastructureLoopRule(IRuleConfigurationSource configurationSource)
    {
        this.configuration = configurationSource.GetConfiguration(Info);
        this.Descriptor = RuleDescriptorFactory.Create(Info, this.configuration);
        this.isEnabled = this.configuration.IsEnabled;
    }

    /// <summary>
    /// Gets the default descriptor used by the rule absent any configuration overrides.
    /// </summary>
    public static DiagnosticDescriptor DefaultDescriptor => DefaultRuleDescriptor;

    /// <summary>
    /// Gets the descriptor instance configured for the current compilation.
    /// </summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>
    /// Registers syntax node actions that inspect loop bodies for infrastructure usage.
    /// </summary>
    /// <param name="context">The compilation start analysis context.</param>
    public void Register(CompilationStartAnalysisContext context)
    {
        if (!this.isEnabled || this.configuration.Severity == DiagnosticSeverity.Hidden)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(this.AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(this.AnalyzeCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(this.AnalyzeImplicitCreation, SyntaxKind.ImplicitObjectCreationExpression);
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
            if (descendant is InvocationExpressionSyntax invocation)
            {
                var symbol = semanticModel.GetSymbolInfo(invocation);
                if (TryGetInfrastructureSymbol(symbol, out _))
                {
                    return true;
                }
            }
            else if (descendant is ObjectCreationExpressionSyntax ||
                     descendant is ImplicitObjectCreationExpressionSyntax)
            {
                var symbol = semanticModel.GetSymbolInfo(descendant);
                if (TryGetInfrastructureSymbol(symbol, out _))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetInfrastructureSymbol(SymbolInfo symbolInfo, out ISymbol? infrastructureSymbol)
    {
        infrastructureSymbol = null;
        if (symbolInfo.Symbol is null)
        {
            return false;
        }

        var ns = symbolInfo.Symbol.ContainingNamespace?.ToDisplayString();
        if (ns is null)
        {
            return false;
        }

        if (ns.IndexOf(InfrastructureNamespaceToken, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            infrastructureSymbol = symbolInfo.Symbol;
            return true;
        }

        return false;
    }

    private static bool IsInsideLoopBody(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (!LoopKinds.Contains(ancestor.Kind()))
            {
                continue;
            }

            var loopBody = GetLoopBody(ancestor);
            if (loopBody is null)
            {
                continue;
            }

            if (loopBody == node || loopBody.FullSpan.Contains(node.FullSpan))
            {
                return true;
            }
        }

        return false;
    }

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

    /// <param name="context">The syntax node analysis context.</param>
    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (!IsInsideLoopBody(invocation))
        {
            return;
        }

        if (!TryGetInfrastructureSymbol(context.SemanticModel.GetSymbolInfo(invocation), out var infrastructureSymbol))
        {
            infrastructureSymbol = this.FindInfrastructureMethod(
                context.SemanticModel.GetSymbolInfo(invocation),
                context.SemanticModel.Compilation);
            if (infrastructureSymbol is null)
            {
                return;
            }
        }

        if (infrastructureSymbol is null)
        {
            return;
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

        context.ReportDiagnostic(Diagnostic.Create(this.Descriptor, location));
    }

    /// <param name="context">The syntax node analysis context.</param>
    private void AnalyzeCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        if (!IsInsideLoopBody(creation))
        {
            return;
        }

        var creationType = context.SemanticModel.GetSymbolInfo(creation.Type).Symbol as ITypeSymbol
                            ?? context.SemanticModel.GetTypeInfo(creation).Type;
        if (creationType is null || !IsInfrastructureSymbol(creationType))
        {
            return;
        }

#if DEBUG
        var symbolDescription = creationType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        Debug.WriteLine($"[CR0003] Creation '{creation}' of infrastructure '{symbolDescription}' is inside a loop.");
#endif

        context.ReportDiagnostic(Diagnostic.Create(this.Descriptor, creation.GetLocation()));
    }

    /// <param name="context">The syntax node analysis context.</param>
    private void AnalyzeImplicitCreation(SyntaxNodeAnalysisContext context)
    {
        var implicitCreation = (ImplicitObjectCreationExpressionSyntax)context.Node;
        if (!IsInsideLoopBody(implicitCreation))
        {
            return;
        }

        var implicitType = context.SemanticModel.GetTypeInfo(implicitCreation).Type;
        if (implicitType is null || !IsInfrastructureSymbol(implicitType))
        {
            return;
        }

#if DEBUG
        var symbolDescription = implicitType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        Debug.WriteLine($"[CR0003] Implicit creation '{implicitCreation}' of infrastructure '{symbolDescription}' is inside a loop.");
#endif

        context.ReportDiagnostic(Diagnostic.Create(this.Descriptor, implicitCreation.GetLocation()));
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

                    var indirectSymbol = this.FindInfrastructureMethod(invocationSymbol, semanticModel.Compilation);
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

    private IMethodSymbol? FindInfrastructureMethod(SymbolInfo symbolInfo, Compilation compilation)
    {
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol &&
            this.CallsMethodWithInfrastructure(methodSymbol, compilation))
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
                this.CallsMethodWithInfrastructure(candidateMethod, compilation))
            {
                return candidateMethod;
            }
        }

        return null;
    }

    private bool CallsMethodWithInfrastructure(IMethodSymbol methodSymbol, Compilation compilation)
    {
        return this.methodInfrastructureCache.GetOrAdd(methodSymbol, symbol =>
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
}
