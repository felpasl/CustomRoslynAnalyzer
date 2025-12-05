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
using CustomRoslynAnalyzer.Core;
using Microsoft.CodeAnalysis;

/// <summary>
/// Provides helpers for building <see cref="DiagnosticDescriptor"/> instances from rule metadata.
/// </summary>
internal static class RuleDescriptorFactory
{
    /// <summary>
    /// Creates a descriptor for the specified rule using the supplied configuration overrides.
    /// </summary>
    /// <param name="info">Metadata describing the rule.</param>
    /// <param name="configuration">Configuration flags resolved for the rule.</param>
    /// <returns>A descriptor that can be consumed by Roslyn.</returns>
    public static DiagnosticDescriptor Create(RuleDescriptorInfo info, RuleConfiguration configuration)
    {
        return new DiagnosticDescriptor(
            id: info.Id,
            title: info.Title,
            messageFormat: info.MessageFormat,
            category: info.Category,
            defaultSeverity: configuration.Severity,
            isEnabledByDefault: configuration.IsEnabled,
            description: info.Description,
            helpLinkUri: info.HelpLinkUri);
    }
}
