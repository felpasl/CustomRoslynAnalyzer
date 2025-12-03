using Microsoft.CodeAnalysis;
using CustomRulesAnalyzer.Rules;

namespace CustomRulesAnalyzer.Rules.Configuration;

internal sealed class RuleConfiguration
{
    public RuleConfiguration(bool isEnabled, DiagnosticSeverity severity)
    {
        IsEnabled = isEnabled;
        Severity = severity;
    }

    public bool IsEnabled { get; }
    public DiagnosticSeverity Severity { get; }

    public RuleConfiguration WithEnabled(bool isEnabled) =>
        new(isEnabled, Severity);

    public RuleConfiguration WithSeverity(DiagnosticSeverity severity) =>
        new(IsEnabled, severity);

    public static RuleConfiguration FromDefaults(RuleDescriptorInfo info) =>
        new(info.EnabledByDefault, info.DefaultSeverity);
}
