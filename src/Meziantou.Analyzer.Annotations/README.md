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
| `DoNotIgnoreAttribute` | Marks a return value or `out` parameter as must-not-be-ignored. | [MA0060](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0060.md) |
| `CultureInsensitiveTypeAttribute` | Marks a type (or a specific format) as culture-insensitive. | [MA0011](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0011.md), [MA0075](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0075.md), [MA0076](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0076.md), [MA0185](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0185.md) |
| `CultureInsensitiveAttribute` | Marks the value of a method, a property, a field, or a parameter as culture-insensitive. | [MA0011](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0011.md), [MA0075](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0075.md), [MA0076](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0076.md), [MA0185](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0185.md) |
| `NonAwaitableTypeAttribute` | Excludes await recommendations for specific types. | [MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md), [MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md), [MA0134](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0134.md), [MA0137](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0137.md), [MA0138](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0138.md) |
| `NonAsyncDisposableTypeAttribute` | Excludes `await using` recommendations for specific types. | [MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md), [MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md) |
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

Use `NonAwaitableTypeAttribute` to exclude MA0042/MA0045 `await` recommendations for specific types at the assembly level.
The match is exact-type only. Derived types are not excluded unless explicitly listed.

```csharp
[assembly: Meziantou.Analyzer.Annotations.NonAwaitableTypeAttribute(typeof(System.Data.Common.DbCommand))]
```

## NonAsyncDisposableTypeAttribute

Use `NonAsyncDisposableTypeAttribute` to exclude MA0042/MA0045 `await using` recommendations for specific types at the assembly level.
The match is exact-type only. Derived types are not excluded unless explicitly listed.

```csharp
[assembly: Meziantou.Analyzer.Annotations.NonAsyncDisposableTypeAttribute(typeof(System.Data.Common.DbCommand))]
```

## CultureInsensitiveAttribute

Use `CultureInsensitiveAttribute` to mark the value of a method, a property, a field, or a parameter as culture-insensitive, even when its type is culture-sensitive.

```csharp
class Sample
{
    [Meziantou.Analyzer.Annotations.CultureInsensitive]
    public static double GetInvariantValue() => 0;
}

_ = $"{Sample.GetInvariantValue()}"; // No MA0076 diagnostic
```

A parameter marked with the attribute is culture-insensitive in both directions: reading it in the method does not report a diagnostic, and MA0075/MA0076 are not reported for the arguments provided by the callers.

```csharp
static void Log([Meziantou.Analyzer.Annotations.CultureInsensitive] string message) { }

Log($"Value: {1.5}"); // No MA0076 diagnostic
```

Members of another assembly can be annotated at the assembly level using their XML documentation id. When the id does not contain the parameter list of a method, all its overloads are annotated.

```csharp
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveAttribute("M:Sample.GetInvariantValue")]
```
