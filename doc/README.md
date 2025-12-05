# Custom Roslyn Analyzer Rules

This directory contains documentation for the custom Roslyn analyzer rules implemented in this project.

## Rules

### CR0001: Avoid Console.WriteLine
Discourages direct use of `Console.WriteLine` in favor of logging abstractions.

[Read more](CR0001.md)

### CR0002: Async method names should end with Async
Enforces that public asynchronous methods end with "Async".

[Read more](CR0002.md)

### CR0003: Avoid infrastructure calls inside loops
Warns against calling infrastructure services from within loop bodies.

[Read more](CR0003.md)

## Configuration

These rules can be configured in your `.editorconfig` file:

```ini
# Enable/disable rules
custom_rules.CR0001.enabled = true
custom_rules.CR0002.enabled = true
custom_rules.CR0003.enabled = true

# Set severity levels
custom_rules.CR0001.severity = warning
custom_rules.CR0002.severity = warning
custom_rules.CR0003.severity = warning
```

## Contributing

When adding new rules, create corresponding documentation files following the format of existing rules.</content>
<parameter name="filePath">/workspaces/RoselynCustomAnalyzer/doc/README.md