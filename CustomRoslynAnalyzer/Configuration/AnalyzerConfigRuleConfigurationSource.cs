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
using Microsoft.CodeAnalysis.Diagnostics;
using System;

/// <summary>
/// Reads rule configuration overrides from analyzer config (.editorconfig) options.
/// </summary>
internal sealed class AnalyzerConfigRuleConfigurationSource : IRuleConfigurationSource
{
    private readonly AnalyzerConfigOptionsProvider optionsProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzerConfigRuleConfigurationSource"/> class.
    /// </summary>
    /// <param name="optionsProvider">The analyzer config options provider.</param>
    public AnalyzerConfigRuleConfigurationSource(AnalyzerConfigOptionsProvider optionsProvider)
    {
        this.optionsProvider = optionsProvider;
    }

    /// <inheritdoc />
    public RuleConfiguration GetConfiguration(RuleDescriptorInfo info)
    {
        var configuration = RuleConfiguration.FromDefaults(info);
        var globalOptions = this.optionsProvider.GlobalOptions;
        var prefix = $"custom_rules.{info.Id}";

        if (AnalyzerConfigRuleConfigurationSource.TryGetBool(globalOptions, $"{prefix}.enabled", out var isEnabled))
        {
            configuration = configuration.WithEnabled(isEnabled);
        }

        if (AnalyzerConfigRuleConfigurationSource.TryGetSeverity(globalOptions, $"{prefix}.severity", out var severity))
        {
            configuration = configuration.WithSeverity(severity);
        }

        return configuration;
    }

    private static bool TryGetBool(AnalyzerConfigOptions options, string key, out bool value)
    {
        value = default;
        if (!options.TryGetValue(key, out var rawText))
        {
            return false;
        }

        return bool.TryParse(rawText, out value);
    }

    private static bool TryGetSeverity(AnalyzerConfigOptions options, string key, out DiagnosticSeverity severity)
    {
        severity = default;
        if (!options.TryGetValue(key, out var rawText) || string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        return rawText.Trim() switch
        {
            string value when string.Equals(value, "error", StringComparison.OrdinalIgnoreCase) => SetSeverity(DiagnosticSeverity.Error, ref severity),
            string value when string.Equals(value, "warning", StringComparison.OrdinalIgnoreCase) => SetSeverity(DiagnosticSeverity.Warning, ref severity),
            string value when string.Equals(value, "info", StringComparison.OrdinalIgnoreCase) => SetSeverity(DiagnosticSeverity.Info, ref severity),
            string value when string.Equals(value, "hidden", StringComparison.OrdinalIgnoreCase) => SetSeverity(DiagnosticSeverity.Hidden, ref severity),
            _ => false
        };
    }

    private static bool SetSeverity(DiagnosticSeverity candidate, ref DiagnosticSeverity severity)
    {
        severity = candidate;
        return true;
    }
}
