using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CustomRulesAnalyzer.Rules.Configuration;

internal sealed class AnalyzerConfigRuleConfigurationSource : IRuleConfigurationSource
{
    private readonly AnalyzerConfigOptionsProvider _optionsProvider;

    public AnalyzerConfigRuleConfigurationSource(AnalyzerConfigOptionsProvider optionsProvider)
    {
        _optionsProvider = optionsProvider;
    }

    public RuleConfiguration GetConfiguration(RuleDescriptorInfo info)
    {
        var configuration = RuleConfiguration.FromDefaults(info);
        var globalOptions = _optionsProvider.GlobalOptions;
        var prefix = $"custom_rules.{info.Id}";

        if (TryGetBool(globalOptions, $"{prefix}.enabled", out var isEnabled))
        {
            configuration = configuration.WithEnabled(isEnabled);
        }

        if (TryGetSeverity(globalOptions, $"{prefix}.severity", out var severity))
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
