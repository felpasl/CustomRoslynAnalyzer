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

/// <summary>
/// Provides configuration settings for analyzer rules at runtime.
/// </summary>
public interface IRuleConfigurationSource
{
    /// <summary>
    /// Retrieves the configuration to apply for the specified rule descriptor.
    /// </summary>
    /// <param name="info">Descriptor metadata describing the analyzer rule.</param>
    /// <returns>The resolved configuration for the target rule.</returns>
    RuleConfiguration GetConfiguration(RuleDescriptorInfo info);
}
