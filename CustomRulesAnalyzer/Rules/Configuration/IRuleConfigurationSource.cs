using CustomRulesAnalyzer.Rules;

namespace CustomRulesAnalyzer.Rules.Configuration;

internal interface IRuleConfigurationSource
{
    RuleConfiguration GetConfiguration(RuleDescriptorInfo info);
}
