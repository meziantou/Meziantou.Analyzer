# Meziantou.Analyzer.Annotations

`Meziantou.Analyzer.Annotations` enables you to configure certain analyzer rules by adding annotations directly to your code.

`Meziantou.Analyzer.Annotations` is a separate dependency from `Meziantou.Analyzer`. If you want to use these attributes in your code, add an explicit package reference:

```bash
dotnet add package Meziantou.Analyzer.Annotations
```

You can also copy the attribute source files into your project as long as the namespace and type names match. The copied types can be `public` or `internal`.

By default, all usages of attributes from `Meziantou.Analyzer.Annotations` are removed from the compiled assembly metadata. This means your binaries will not reference the `Meziantou.Analyzer.Annotations.dll` assembly.

If you want to keep these attributes in the metadata (for example, for reflection or tooling purposes), define the `MEZIANTOU_ANALYZER_ANNOTATIONS` conditional compilation symbol in your project settings.

## Available attributes

| Attribute | Purpose | Related rules |
| --- | --- | --- |
| `CultureInsensitiveTypeAttribute` | Marks a type (or a specific format) as culture-insensitive. | [MA0011](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0011.md), [MA0075](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0075.md), [MA0076](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0076.md), [MA0185](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0185.md) |
| `NonAwaitableTypeAttribute` | Excludes await/await-using recommendations for specific types. | [MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md), [MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md) |
| `ExcludeFromBlockingCallAnalysisAttribute` | Excludes specific methods/properties from blocking-call diagnostics. | [MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md), [MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md) |
| `RequireNamedArgumentAttribute` | Requires named arguments for decorated parameters. | [MA0003](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0003.md) |
| `StructuredLogFieldAttribute` | Declares allowed types for named log properties in an assembly. | [MA0124](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0124.md), [MA0139](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0139.md) |

## ExcludeFromBlockingCallAnalysisAttribute

Use `ExcludeFromBlockingCallAnalysisAttribute` to exclude specific MA0042/MA0045 method/property diagnostics at the assembly level.

```csharp
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("M:System.Threading.Tasks.Task.Wait")]
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute(typeof(System.Threading.Thread), "Sleep", typeof(int))]
```

## NonAwaitableTypeAttribute

Use `NonAwaitableTypeAttribute` to exclude MA0042/MA0045 `await`/`await using` recommendations for specific types at the assembly level.

```csharp
[assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(System.Data.Common.DbCommand))]
```
