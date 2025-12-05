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
using System.Collections.Immutable;

/// <summary>
/// Analyzer rule that discourages direct usage of <see cref="System.Console.WriteLine(string)"/> in favor of logging abstractions.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class AvoidConsoleWriteLineRule : DiagnosticAnalyzer
{
    private const string DiagnosticId = "CR0001";
    private const string Title = "Avoid Console.WriteLine";
    private const string MessageFormat = "Use a logging abstraction instead of Console.WriteLine";
    private const string Category = "Usage";
    private const string Description =
        "Console.WriteLine makes automated testing harder. Use ILogger or another abstraction instead.";

    private static readonly DiagnosticDescriptor Rule = new (
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/felpasl/CustomRoselynAnalyzer/blob/main/doc/CR0001.md");

    /// <summary>
    /// Gets the default descriptor used when no configuration overrides are provided.
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

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    /// <param name="context">The syntax node analysis context.</param>
    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
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
            (symbol.Name == "WriteLine" || symbol.Name == "Write"))
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation()));
        }
    }
}
