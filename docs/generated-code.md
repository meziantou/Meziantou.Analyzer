# Analyzing generated code

The rules skip generated code, which is the expected behavior for the vast majority of projects: you cannot fix code
you do not own, and analyzing code nobody looks at costs build time. Some rules are the exception and analyze it
anyway: the ones whose subject is the generated file itself, such as the Blazor rules that work on the code generated
from the `.razor` files, and the ones that need to see the whole compilation to be correct. Set the
`MEZIANTOU_ANALYZER_GENERATED_CODE` environment variable to opt in to analyzing generated code with every rule.

## Configuration

The `report_generated_code` option indicates whether a rule reports the diagnostics located in the generated code it
analyzes. It can be set for a single rule, or for all of them at once with the `MA` prefix:

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

This option only decides what a rule reports, not what it analyzes, so `report_generated_code = true` has no effect on
a rule that does not analyze generated code: it needs the environment variable below. Setting it to `false` always
works, as a rule can only report what it is allowed to report.

## Opting in with the environment variable

Set the `MEZIANTOU_ANALYZER_GENERATED_CODE` environment variable to analyze generated code with every rule:

| Value | Behavior |
|-------|----------|
| `true` (case-insensitive) or `1` | The rules analyze generated code, and `report_generated_code` decides what they report |
| not set, empty, or any other value | The rules skip generated code, except the ones that need it |

Two kinds of rules analyze generated code without the variable: the ones that report in generated code by default,
listed below, and the ones that need to see the whole compilation to be correct, such as MA0053, which reports a class
that no other class inherits from and would report a false positive if the deriving class were declared in a generated
file. `report_generated_code` works on those rules whether the variable is set or not.

## Rules reporting in generated code by default

<!-- rules -->

|Id|Description|
|--|-----------|
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

The rules that do not analyze generated code never see it, so they follow the detection of Roslyn instead, which also
considers the symbols marked with `[GeneratedCode]` or `[DebuggerNonUserCode]` generated. The two detections only
differ for a rule the variable opted in.

## Using the `generated_code` option

Roslyn supports the `generated_code` option, which overrides the detection above for a set of files. Unlike
`report_generated_code` and the environment variable, it applies to **all** the analyzers, not only to
Meziantou.Analyzer:

```ini
[Generated/**.cs]
generated_code = false
```

Use `report_generated_code` when you want Meziantou.Analyzer specifically to report everything Roslyn considers
generated, and `generated_code` when you want all the analyzers to treat a specific set of files as regular code.

## Why an environment variable

An analyzer must declare how it handles generated code from `Initialize(AnalysisContext)`, and the options of the
`.editorconfig` files are not available at that point: they can only be read from the analysis callbacks, which run
later. An environment variable is the only configuration that can be read early enough, which is why the global
opt-in is not an `.editorconfig` option.

This has consequences that are worth knowing:

- **The compiler server caches the value.** `dotnet build` and `msbuild` run the analyzers inside `VBCSCompiler`,
  which is reused across builds and keeps the environment it was started with. Run `dotnet build-server shutdown`
  after changing the variable.
- **The IDEs cache it too.** Visual Studio, Rider, and the C# extension of Visual Studio Code run the analyzers in
  their own long-lived processes. Restart the IDE after changing the variable.
- **Changing the variable does not invalidate the build.** It is not a compiler input, so MSBuild considers the
  projects up-to-date and skips the compilation. Rebuild the solution to see the new diagnostics.
- **There is no MSBuild property.** MSBuild can only pass environment variables to a process it starts itself,
  which is not the case when the compilation is delegated to the compiler server. A property would work on some
  machines and silently do nothing on others.
