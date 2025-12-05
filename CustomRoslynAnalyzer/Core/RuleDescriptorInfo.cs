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

namespace CustomRoslynAnalyzer.Core;
using Microsoft.CodeAnalysis;

/// <summary>
/// Immutable metadata describing how a diagnostic rule presents itself to users.
/// </summary>
public sealed class RuleDescriptorInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RuleDescriptorInfo"/> class.
    /// </summary>
    /// <param name="id">The diagnostic identifier (e.g., CR0001).</param>
    /// <param name="title">The short title displayed with the diagnostic.</param>
    /// <param name="messageFormat">The message format template for diagnostic instances.</param>
    /// <param name="category">The diagnostic category (Usage, Naming, etc.).</param>
    /// <param name="defaultSeverity">The default severity applied when the rule is enabled.</param>
    /// <param name="enabledByDefault">A value indicating whether the rule is enabled by default.</param>
    /// <param name="description">The detailed description shown in IDEs and documentation.</param>
    /// <param name="helpLinkUri">The URI to documentation for this rule.</param>
    public RuleDescriptorInfo(
        string id,
        string title,
        string messageFormat,
        string category,
        DiagnosticSeverity defaultSeverity,
        bool enabledByDefault,
        string description,
        string? helpLinkUri = null)
    {
        this.Id = id;
        this.Title = title;
        this.MessageFormat = messageFormat;
        this.Category = category;
        this.DefaultSeverity = defaultSeverity;
        this.EnabledByDefault = enabledByDefault;
        this.Description = description;
        this.HelpLinkUri = helpLinkUri;
    }

    /// <summary>
    /// Gets the diagnostic identifier (e.g., CR0001).
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the short title displayed with the diagnostic.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the message format template for diagnostic instances.
    /// </summary>
    public string MessageFormat { get; }

    /// <summary>
    /// Gets the diagnostic category (Usage, Naming, etc.).
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets the default severity applied when the rule is enabled.
    /// </summary>
    public DiagnosticSeverity DefaultSeverity { get; }

    /// <summary>
    /// Gets a value indicating whether the rule is enabled by default.
    /// </summary>
    public bool EnabledByDefault { get; }

    /// <summary>
    /// Gets the detailed description shown in IDEs and documentation.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the URI to documentation for this rule.
    /// </summary>
    public string? HelpLinkUri { get; }
}
