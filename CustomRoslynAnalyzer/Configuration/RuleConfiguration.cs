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
/// Represents the effective configuration for a single analyzer rule.
/// </summary>
public sealed class RuleConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleConfiguration"/> class.
    /// </summary>
    /// <param name="isEnabled">Whether the rule should be evaluated.</param>
    /// <param name="severity">The diagnostic severity to use when reporting.</param>
    public RuleConfiguration(bool isEnabled, DiagnosticSeverity severity)
    {
        this.IsEnabled = isEnabled;
        this.Severity = severity;
    }

    /// <summary>
    /// Gets a value indicating whether the rule is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the severity at which diagnostics should be reported.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    /// Creates a configuration using the defaults defined on the descriptor.
    /// </summary>
    /// <param name="info">The rule descriptor metadata.</param>
    /// <returns>A configuration initialized from the descriptor defaults.</returns>
    public static RuleConfiguration FromDefaults(RuleDescriptorInfo info) =>
        new (info.EnabledByDefault, info.DefaultSeverity);

    /// <summary>
    /// Returns a copy of this configuration with a different enabled flag.
    /// </summary>
    /// <param name="isEnabled">The enabled flag to set on the new configuration.</param>
    /// <returns>A copy of this configuration with the enabled flag set to the specified value.</returns>
    public RuleConfiguration WithEnabled(bool isEnabled) =>
        new (isEnabled, this.Severity);

    /// <summary>
    /// Returns a copy of this configuration with a different severity.
    /// </summary>
    /// <param name="severity">The severity to set on the new configuration.</param>
    /// <returns>A copy of this configuration with the severity set to the specified value.</returns>
    public RuleConfiguration WithSeverity(DiagnosticSeverity severity) =>
        new (this.IsEnabled, severity);
}
