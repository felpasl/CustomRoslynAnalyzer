using CustomRulesAnalyzer.Rules;

namespace CustomRulesAnalyzer.Rules.Configuration;

internal sealed class StaticRuleConfigurationSource : IRuleConfigurationSource
{
    public RuleConfiguration GetConfiguration(RuleDescriptorInfo info) =>
        RuleConfiguration.FromDefaults(info);
}
