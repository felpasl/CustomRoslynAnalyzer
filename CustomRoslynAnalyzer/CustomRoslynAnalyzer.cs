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

namespace CustomRoslynAnalyzer;
using CustomRoslynAnalyzer.Configuration;
using CustomRoslynAnalyzer.Core;
using CustomRoslynAnalyzer.Rules;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

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
                new NestedInfrastructureLoopRule(configurationSource),
            };

            foreach (var rule in rules)
            {
                rule.Register(compilationContext);
            }
        });
    }
}
