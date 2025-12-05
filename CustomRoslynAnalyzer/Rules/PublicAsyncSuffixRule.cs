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
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

/// <summary>
/// Analyzer rule that enforces the 'Async' suffix on public asynchronous method names.
/// </summary>
public sealed class PublicAsyncSuffixRule : IAnalyzerRule
{
    private const string DiagnosticId = "CR0002";
    private const string Title = "Async method names should end with Async";
    private const string MessageFormat = "Rename '{0}' to end with Async to clarify asynchronous usage";
    private const string Category = "Naming";
    private const string Description =
        "Async methods should end with Async so consumers understand they run asynchronously.";

    private static readonly RuleDescriptorInfo Info = new (
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        enabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/felpasl/CustomRoselynAnalyzer/blob/main/doc/CR0002.md");

    private static readonly DiagnosticDescriptor DefaultRuleDescriptor = new (
        id: DiagnosticId,
        title: Title,
        messageFormat: MessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description,
        helpLinkUri: "https://github.com/felpasl/CustomRoselynAnalyzer/blob/main/doc/CR0002.md");

    private readonly bool isEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublicAsyncSuffixRule"/> class.
    /// </summary>
    /// <param name="configurationSource">The source for rule configuration.</param>
    public PublicAsyncSuffixRule(IRuleConfigurationSource configurationSource)
    {
        var configuration = configurationSource.GetConfiguration(Info);
        this.Descriptor = RuleDescriptorFactory.Create(Info, configuration);
        this.isEnabled = configuration.IsEnabled;
    }

    /// <summary>
    /// Gets the descriptor used when the rule uses default configuration.
    /// </summary>
    public static DiagnosticDescriptor DefaultDescriptor => DefaultRuleDescriptor;

    /// <summary>
    /// Gets the descriptor instance configured for the active compilation.
    /// </summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>
    /// Registers analysis callbacks for method symbols.
    /// </summary>
    /// <param name="context">The compilation start analysis context.</param>
    public void Register(CompilationStartAnalysisContext context)
    {
        if (!this.isEnabled)
        {
            return;
        }

        context.RegisterSymbolAction(this.AnalyzeMethodSymbol, SymbolKind.Method);
    }

    private static bool ReturnsTask(IMethodSymbol methodSymbol)
    {
        var returnType = methodSymbol.ReturnType;
        return returnType.Name is "Task" or "ValueTask";
    }

    /// <param name="context">The symbol analysis context.</param>
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
            context.ReportDiagnostic(Diagnostic.Create(this.Descriptor, location, methodSymbol.Name));
        }
    }
}
