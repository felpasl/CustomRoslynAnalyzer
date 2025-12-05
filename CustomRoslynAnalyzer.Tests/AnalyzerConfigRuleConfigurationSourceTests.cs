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

using CustomRoslynAnalyzer.Configuration;
using CustomRoslynAnalyzer.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace CustomRoslynAnalyzer.Tests;

public sealed class AnalyzerConfigRuleConfigurationSourceTests
{
    private static readonly RuleDescriptorInfo TestRuleInfo = new(
        id: "CR0001",
        title: "Test Rule",
        messageFormat: "Test message",
        category: "Test",
        defaultSeverity: DiagnosticSeverity.Warning,
        enabledByDefault: true,
        description: "Test description");

    [Fact]
    public void GetConfiguration_ReturnsDefault_WhenNoConfigProvided()
    {
        var optionsProvider = new TestAnalyzerConfigOptionsProvider();
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.True(config.IsEnabled);
        Assert.Equal(DiagnosticSeverity.Warning, config.Severity);
    }

    [Fact]
    public void GetConfiguration_SetsSeverityToError_WhenConfigSpecifiesError()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "error");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.True(config.IsEnabled);
        Assert.Equal(DiagnosticSeverity.Error, config.Severity);
    }

    [Fact]
    public void GetConfiguration_SetsSeverityToWarning_WhenConfigSpecifiesWarning()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "warning");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.True(config.IsEnabled);
        Assert.Equal(DiagnosticSeverity.Warning, config.Severity);
    }

    [Fact]
    public void GetConfiguration_SetsSeverityToInfo_WhenConfigSpecifiesInfo()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "info");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.True(config.IsEnabled);
        Assert.Equal(DiagnosticSeverity.Info, config.Severity);
    }

    [Fact]
    public void GetConfiguration_SetsSeverityToHidden_WhenConfigSpecifiesHidden()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "hidden");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.True(config.IsEnabled);
        Assert.Equal(DiagnosticSeverity.Hidden, config.Severity);
    }

    [Fact]
    public void GetConfiguration_IgnoresCase_WhenConfigSpecifiesSeverity()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "ERROR");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.Equal(DiagnosticSeverity.Error, config.Severity);
    }

    [Fact]
    public void GetConfiguration_TrimsWhitespace_WhenConfigSpecifiesSeverity()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "  error  ");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.Equal(DiagnosticSeverity.Error, config.Severity);
    }

    [Fact]
    public void GetConfiguration_ReturnsDefaultSeverity_WhenConfigSpecifiesInvalidSeverity()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "invalid");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.Equal(DiagnosticSeverity.Warning, config.Severity);
    }

    [Fact]
    public void GetConfiguration_ReturnsDefaultSeverity_WhenConfigSpecifiesEmptySeverity()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.Equal(DiagnosticSeverity.Warning, config.Severity);
    }

    [Fact]
    public void GetConfiguration_ReturnsDefaultSeverity_WhenConfigSpecifiesWhitespaceOnlySeverity()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.severity", "   ");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.Equal(DiagnosticSeverity.Warning, config.Severity);
    }

    [Fact]
    public void GetConfiguration_SetsEnabledToFalse_WhenConfigSpecifiesDisabled()
    {
        var options = new TestAnalyzerConfigOptions();
        options.SetValue("custom_rules.CR0001.enabled", "false");
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(options);
        var source = new AnalyzerConfigRuleConfigurationSource(optionsProvider);

        var config = source.GetConfiguration(TestRuleInfo);

        Assert.False(config.IsEnabled);
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> values = new();

        public void SetValue(string key, string value) => this.values[key] = value;
        public override bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => this.values.TryGetValue(key, out value);
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly AnalyzerConfigOptions globalOptions;

        public TestAnalyzerConfigOptionsProvider(AnalyzerConfigOptions? globalOptions = null)
        {
            this.globalOptions = globalOptions ?? new TestAnalyzerConfigOptions();
        }

        public override AnalyzerConfigOptions GlobalOptions => this.globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new NotImplementedException();

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => throw new NotImplementedException();
    }
}