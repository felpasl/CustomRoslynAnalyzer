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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;

/// <summary>
/// Analyzer rule that prevents service or repository types from being used inside foreach loops or lambda bodies.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AvoidServiceRepositoryInIterationRule : DiagnosticAnalyzer
{
    private const string DiagnosticId = "CR0004";
    private const string Title = "Avoid Service/Repository usage inside iterations";
    private const string MessageFormat = "Avoid referencing '{0}' inside foreach loops or lambda iterations";
    private const string Category = "Usage";
    private const string Description =
        "Service or repository dependencies should not be invoked directly inside foreach or lambda-based iterations.";

    private static readonly DiagnosticDescriptor Rule = new (
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/felpasl/CustomRoslynAnalyzer/blob/main/doc/CR0004.md");

    private static readonly SyntaxKind[] LoopKinds =
    {
        SyntaxKind.ForEachStatement,
        SyntaxKind.ForEachVariableStatement,
    };

    /// <summary>
    /// Gets the diagnostic descriptor exposed by the rule.
    /// </summary>
    public static DiagnosticDescriptor DefaultDescriptor => Rule;

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var methodUsageCache = new ConcurrentDictionary<IMethodSymbol, ISymbol?>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeInvocation(nodeContext, compilationContext.Compilation, methodUsageCache),
                SyntaxKind.InvocationExpression);
            compilationContext.RegisterSyntaxNodeAction(
                AnalyzeCreation,
                SyntaxKind.ObjectCreationExpression);
            compilationContext.RegisterSyntaxNodeAction(
                AnalyzeImplicitCreation,
                SyntaxKind.ImplicitObjectCreationExpression);
        });
    }

    private static void AnalyzeInvocation(
        SyntaxNodeAnalysisContext context,
        Compilation compilation,
        ConcurrentDictionary<IMethodSymbol, ISymbol?> methodUsageCache)
    {
        if (context.Node is not InvocationExpressionSyntax invocation ||
            !IsInRestrictedContext(invocation, context.SemanticModel))
        {
            return;
        }

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
        if (!TryGetServiceRepositorySymbol(symbolInfo, out var targetSymbol))
        {
            targetSymbol = FindMethodWithServiceUsage(symbolInfo, compilation, methodUsageCache);
            if (targetSymbol is null)
            {
                return;
            }
        }

        if (targetSymbol is null)
        {
            return;
        }

        var location = invocation.GetLocation();
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            location = memberAccess.Name.GetLocation();
        }
        else if (invocation.Expression is IdentifierNameSyntax identifierName)
        {
            location = identifierName.GetLocation();
        }

        var diagnostic = Diagnostic.Create(Rule, location, GetSymbolDisplayName(targetSymbol));
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ObjectCreationExpressionSyntax creation ||
            !IsInRestrictedContext(creation, context.SemanticModel))
        {
            return;
        }

        var typeSymbolCandidate = context.SemanticModel.GetSymbolInfo(creation.Type).Symbol as ITypeSymbol
                                  ?? context.SemanticModel.GetTypeInfo(creation).Type;
        if (typeSymbolCandidate is not ITypeSymbol typeSymbol ||
            !IsServiceRepositorySymbol(typeSymbol))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, creation.GetLocation(), typeSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeImplicitCreation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ImplicitObjectCreationExpressionSyntax creation ||
            !IsInRestrictedContext(creation, context.SemanticModel))
        {
            return;
        }

        var typeSymbolCandidate = context.SemanticModel.GetTypeInfo(creation).Type;
        if (typeSymbolCandidate is not ITypeSymbol typeSymbol ||
            !IsServiceRepositorySymbol(typeSymbol))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(Rule, creation.GetLocation(), typeSymbol.Name);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool TryGetServiceRepositorySymbol(SymbolInfo symbolInfo, out ISymbol? target)
    {
        target = null;
        if (symbolInfo.Symbol is ISymbol symbol && IsServiceRepositorySymbol(symbol))
        {
            target = symbol;
            return true;
        }

        return false;
    }

    private static bool IsServiceRepositorySymbol(ISymbol? symbol)
    {
        if (symbol is null)
        {
            return false;
        }

        switch (symbol)
        {
            case ITypeSymbol typeSymbol:
                return HasServiceRepositoryName(typeSymbol.Name);
            case IMethodSymbol methodSymbol:
                return HasServiceRepositoryName(methodSymbol.ContainingType?.Name);
            case IPropertySymbol propertySymbol:
                return HasServiceRepositoryName(propertySymbol.ContainingType?.Name);
            case IFieldSymbol fieldSymbol:
                return HasServiceRepositoryName(fieldSymbol.ContainingType?.Name);
            case IEventSymbol eventSymbol:
                return HasServiceRepositoryName(eventSymbol.ContainingType?.Name);
        }

        return HasServiceRepositoryName(symbol.Name);
    }

    private static bool HasServiceRepositoryName(string? name)
    {
        if (name is not { Length: > 0 })
        {
            return false;
        }

        return name.IndexOf("Service", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Repository", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsInRestrictedContext(SyntaxNode node, SemanticModel semanticModel)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (LoopKinds.Contains(ancestor.Kind()))
            {
                return true;
            }

            if (ancestor is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax)
            {
                var invocation = ancestor.Ancestors()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault();

                if (invocation is null)
                {
                    continue;
                }

                var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol
                                   ?? semanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
                if (IsIterationLambdaMethod(methodSymbol))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsIterationLambdaMethod(IMethodSymbol? methodSymbol)
    {
        if (methodSymbol is null)
        {
            return false;
        }

        if (methodSymbol.Name == "ForEach")
        {
            return true;
        }

        if (methodSymbol.Name is "Select" or "Where")
        {
            var containingType = methodSymbol.ContainingType?.ToDisplayString();
            return string.Equals(containingType, "System.Linq.Enumerable", StringComparison.Ordinal) ||
                   string.Equals(containingType, "System.Linq.Queryable", StringComparison.Ordinal);
        }

        return false;
    }

    private static ISymbol? FindMethodWithServiceUsage(
        SymbolInfo symbolInfo,
        Compilation compilation,
        ConcurrentDictionary<IMethodSymbol, ISymbol?> methodUsageCache)
    {
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            var dependency = GetServiceRepositoryDependency(methodSymbol, compilation, methodUsageCache);
            if (dependency is not null)
            {
                return dependency;
            }
        }

        if (symbolInfo.CandidateReason == CandidateReason.None)
        {
            return null;
        }

        foreach (var candidate in symbolInfo.CandidateSymbols)
        {
            if (candidate is IMethodSymbol candidateMethod)
            {
                var dependency = GetServiceRepositoryDependency(candidateMethod, compilation, methodUsageCache);
                if (dependency is not null)
                {
                    return dependency;
                }
            }
        }

        return null;
    }

    private static ISymbol? GetServiceRepositoryDependency(
        IMethodSymbol methodSymbol,
        Compilation compilation,
        ConcurrentDictionary<IMethodSymbol, ISymbol?> methodUsageCache)
    {
        return methodUsageCache.GetOrAdd(methodSymbol, symbol =>
        {
            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var syntax = syntaxReference.GetSyntax();
#pragma warning disable RS1030 // Need semantic model to inspect method bodies for dependency traversal.
                var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);
#pragma warning restore RS1030
                var body = ExtractBodyNode(syntax);
                if (body is null)
                {
                    continue;
                }

                if (TryFindServiceRepositoryUsage(body, semanticModel, out var dependency))
                {
                    return dependency;
                }
            }

            return null;
        });
    }

    private static bool TryFindServiceRepositoryUsage(
        SyntaxNode node,
        SemanticModel model,
        out ISymbol? dependency)
    {
        foreach (var descendant in node.DescendantNodesAndSelf())
        {
            if (descendant is InvocationExpressionSyntax invocation)
            {
                var symbol = model.GetSymbolInfo(invocation).Symbol;
                if (IsServiceRepositorySymbol(symbol))
                {
                    dependency = symbol;
                    return true;
                }
            }
            else if (descendant is ObjectCreationExpressionSyntax creation)
            {
                var typeSymbolCandidate = model.GetSymbolInfo(creation.Type).Symbol as ITypeSymbol
                                          ?? model.GetTypeInfo(creation).Type;
                if (typeSymbolCandidate is ITypeSymbol typeSymbol &&
                    IsServiceRepositorySymbol(typeSymbol))
                {
                    dependency = typeSymbol;
                    return true;
                }
            }
            else if (descendant is ImplicitObjectCreationExpressionSyntax implicitCreation)
            {
                var implicitTypeCandidate = model.GetTypeInfo(implicitCreation).Type;
                if (implicitTypeCandidate is ITypeSymbol implicitType &&
                    IsServiceRepositorySymbol(implicitType))
                {
                    dependency = implicitType;
                    return true;
                }
            }
        }

        dependency = null;
        return false;
    }

    private static SyntaxNode? ExtractBodyNode(SyntaxNode syntax) => syntax switch
    {
        BaseMethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
        AccessorDeclarationSyntax accessor => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody?.Expression,
        LocalFunctionStatementSyntax localFunction => (SyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody?.Expression,
        _ => null
    };

    private static string GetSymbolDisplayName(ISymbol symbol)
    {
        if (symbol is IMethodSymbol method && method.ContainingType is { } containingType)
        {
            return containingType.Name;
        }

        if (symbol is ITypeSymbol typeSymbol)
        {
            return typeSymbol.Name;
        }

        return symbol.Name;
    }
}
