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

namespace CustomRoslynAnalyzer.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Creates <see cref="IRuleConfigurationSource"/> instances based on available analyzer options.
/// </summary>
internal static class RuleConfigurationSourceFactory
{
    /// <summary>
    /// Creates a configuration source that considers the provided analyzer config options, if any.
    /// </summary>
    /// <param name="optionsProvider">The options provider surfaced by Roslyn, or <c>null</c>.</param>
    /// <returns>An <see cref="IRuleConfigurationSource"/> implementation.</returns>
    public static IRuleConfigurationSource Create(AnalyzerConfigOptionsProvider? optionsProvider)
    {
        if (optionsProvider is null)
        {
            return new StaticRuleConfigurationSource();
        }

        return new AnalyzerConfigRuleConfigurationSource(optionsProvider);
    }
}
