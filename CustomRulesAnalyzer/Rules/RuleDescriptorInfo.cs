using Microsoft.CodeAnalysis;

namespace CustomRulesAnalyzer.Rules;

internal sealed class RuleDescriptorInfo
{
    public RuleDescriptorInfo(
        string id,
        string title,
        string messageFormat,
        string category,
        DiagnosticSeverity defaultSeverity,
        bool enabledByDefault,
        string description)
    {
        Id = id;
        Title = title;
        MessageFormat = messageFormat;
        Category = category;
        DefaultSeverity = defaultSeverity;
        EnabledByDefault = enabledByDefault;
        Description = description;
    }

    public string Id { get; }
    public string Title { get; }
    public string MessageFormat { get; }
    public string Category { get; }
    public DiagnosticSeverity DefaultSeverity { get; }
    public bool EnabledByDefault { get; }
    public string Description { get; }
}
