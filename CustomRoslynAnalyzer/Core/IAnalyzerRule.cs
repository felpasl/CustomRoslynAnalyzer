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
using Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Represents a Roslyn analyzer rule that can register callbacks for a compilation.
/// </summary>
internal interface IAnalyzerRule
{
    /// <summary>
    /// Gets the configured diagnostic descriptor exposed by the rule.
    /// </summary>
    DiagnosticDescriptor Descriptor { get; }

    /// <summary>
    /// Registers analysis callbacks with the provided compilation context.
    /// </summary>
    /// <param name="context">The compilation context used to register actions.</param>
    void Register(CompilationStartAnalysisContext context);
}
