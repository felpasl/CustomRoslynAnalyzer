using Microsoft.CodeAnalysis;
using CustomRulesAnalyzer.Rules;

namespace CustomRulesAnalyzer.Rules.Configuration;

internal static class RuleDescriptorFactory
{
    public static DiagnosticDescriptor Create(RuleDescriptorInfo info, RuleConfiguration configuration)
    {
        return new DiagnosticDescriptor(
            id: info.Id,
            title: info.Title,
            messageFormat: info.MessageFormat,
            category: info.Category,
            defaultSeverity: configuration.Severity,
            isEnabledByDefault: configuration.IsEnabled,
            description: info.Description);
    }
}
