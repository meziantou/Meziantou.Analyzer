# Analyzing generated code

The rules analyze generated code, so they see the whole compilation, but most of them do not report the diagnostics
located in generated code, which is the expected behavior for the vast majority of projects: you cannot fix code you
do not own. Some rules are the exception and report in generated code by default, as the generated file is the
subject of the rule, such as the Blazor rules that work on the code generated from the `.razor` files.

## Configuration

The `report_generated_code` option indicates whether a rule reports the diagnostics located in generated code. It can
be set for a single rule, or for all of them at once with the `MA` prefix:

```ini
[*.cs]
# All the rules report the diagnostics located in generated code
MA.report_generated_code = true

# MA0051 does not, as a rule specific value wins over the value of all the rules
MA0051.report_generated_code = false
```

| Key | Applies to |
|-----|------------|
| `MA.report_generated_code` | All the rules |
| `MA0001.report_generated_code` | The rule `MA0001`, and wins over `MA.report_generated_code` |

Both keys are read from the `.editorconfig` section of the generated file, so they can be set for a specific set of
generated files:

```ini
[Generated/**.cs]
MA0051.report_generated_code = true
```

When neither is set, each rule uses its own default, which is to not report in generated code, except for the rules
below. To turn off a rule entirely, set its severity to `none` instead.

## Rules reporting in generated code by default

<!-- rules -->

|Id|Description|
|--|-----------|
|[MA0004](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0004.md)|Use Task.ConfigureAwait|
|[MA0068](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0068.md)|Invalid parameter name for nullable attribute|
|[MA0070](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0070.md)|Obsolete attributes should include explanations|
|[MA0115](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0115.md)|Unknown component parameter|
|[MA0116](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0116.md)|Parameters with \[SupplyParameterFromQuery\] attributes should also be marked as \[Parameter\]|
|[MA0117](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0117.md)|Parameters with \[EditorRequired\] attributes should also be marked as \[Parameter\]|
|[MA0118](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0118.md)|\[JSInvokable\] methods must be public|
|[MA0119](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0119.md)|JSRuntime must not be used in OnInitialized or OnInitializedAsync|
|[MA0120](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0120.md)|Use InvokeVoidAsync when the returned value is not used|
|[MA0121](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0121.md)|Do not overwrite parameter value|
|[MA0122](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0122.md)|Parameters with \[SupplyParameterFromQuery\] attributes are only valid in routable components (@page)|
|[MA0123](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0123.md)|Sequence number must be a constant|
|[MA0124](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0124.md)|Microsoft.Extensions.Logging parameter type is not valid|
|[MA0125](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0125.md)|The list of log parameter types contains an invalid type|
|[MA0126](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0126.md)|The list of log parameter types contains a duplicate|
|[MA0135](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0135.md)|The log parameter has no configured type|
|[MA0139](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0139.md)|Serilog parameter type is not valid|
|[MA0144](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0144.md)|Use System.OperatingSystem to check the current OS|
|[MA0153](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0153.md)|Do not log symbols decorated with DataClassificationAttribute directly|
|[MA0160](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0160.md)|Use ContainsKey instead of TryGetValue|
|[MA0176](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0176.md)|Optimize guid creation|
|[MA0190](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0190.md)|Use partial property instead of partial method for GeneratedRegex|
|[MA0195](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0195.md)|Do not use static fields before they are initialized|

<!-- rules -->

## What is generated code

A file is considered generated when:

- the `.editorconfig` file sets `generated_code` for the file, which takes precedence over the rest,
- or its name is `*.designer.cs`, `*.generated.cs`, `*.g.cs`, `*.g.i.cs`, or starts with `TemporaryGeneratedFile_`,
- or it starts with an `<auto-generated>` comment.

Only the file of the diagnostic is considered. A `[GeneratedCode]` or `[DebuggerNonUserCode]` attribute in a hand
written file does not make the code generated, and a partial type declared in a generated file and in a hand written
one reports only in the hand written file. Use the `generated_code` option below for the files the detection does not
recognize.

## Using the `generated_code` option

Roslyn supports the `generated_code` option, which overrides the detection above for a set of files. Unlike
`report_generated_code`, it applies to **all** the analyzers, not only to Meziantou.Analyzer:

```ini
[Generated/**.cs]
generated_code = false
```

Use `report_generated_code` when you want Meziantou.Analyzer specifically to report everything Roslyn considers
generated, and `generated_code` when you want all the analyzers to treat a specific set of files as regular code.
