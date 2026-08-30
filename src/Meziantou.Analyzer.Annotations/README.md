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
| `CultureInsensitiveAttribute` | Marks the value of a property, a field, or a parameter as culture-insensitive. | [MA0011](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0011.md), [MA0075](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0075.md), [MA0076](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0076.md), [MA0185](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0185.md) |
| `NonAwaitableTypeAttribute` | Excludes await recommendations for specific types. | [MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md), [MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md), [MA0134](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0134.md), [MA0137](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0137.md), [MA0138](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0138.md) |
| `NonAsyncDisposableTypeAttribute` | Excludes `await using` recommendations for specific types. | [MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md), [MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md) |
| `ExcludeFromBlockingCallAnalysisAttribute` | Excludes specific methods/properties from blocking-call diagnostics. | [MA0042](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0042.md), [MA0045](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0045.md) |
| `ExcludeFromCancellationTokenAnalysisAttribute` | Excludes specific methods from the `CancellationToken` diagnostics. | [MA0032](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0032.md), [MA0040](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0040.md), [MA0079](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0079.md), [MA0080](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0080.md) |
| `RequireNamedArgumentAttribute` | Requires named arguments for decorated parameters. | [MA0003](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0003.md) |
| `StructuredLogFieldAttribute` | Declares allowed types for named log properties in an assembly. | [MA0124](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0124.md), [MA0139](https://github.com/meziantou/Meziantou.Analyzer/blob/main/docs/Rules/MA0139.md) |

## ExcludeFromBlockingCallAnalysisAttribute

Use `ExcludeFromBlockingCallAnalysisAttribute` to exclude specific MA0042/MA0045 method/property diagnostics at the assembly level.

```csharp
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute("M:System.Threading.Tasks.Task.Wait")]
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromBlockingCallAnalysisAttribute(typeof(System.Threading.Thread), "Sleep", typeof(int))]
```

## ExcludeFromCancellationTokenAnalysisAttribute

Use `ExcludeFromCancellationTokenAnalysisAttribute` to mark a method as valid to call without a `CancellationToken`. No MA0032/MA0040/MA0079/MA0080 diagnostic is reported for the calls to an excluded method, even when the method has an overload with a `CancellationToken` and a token is available in the scope.

Methods of the current project can be annotated directly:

```csharp
class Sample
{
    [Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis]
    public static System.Threading.Tasks.Task FlushAsync() => throw null;
    public static System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) => throw null;
}

await Sample.FlushAsync(); // No MA0040 diagnostic
```

Methods of another assembly can be annotated at the assembly level, using their XML documentation id or their containing type and name. The parameter types are optional and restrict the exclusion to a single overload.

```csharp
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis("M:System.Threading.Channels.ChannelWriter`1.WriteAsync(`0,System.Threading.CancellationToken)")]
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis(typeof(Sample), "FlushAsync")]
[assembly: Meziantou.Analyzer.Annotations.ExcludeFromCancellationTokenAnalysis(typeof(Sample), "FlushAsync", typeof(System.Threading.CancellationToken))]
```

Note that the attribute is removed from the metadata of the compiled assembly unless the `MEZIANTOU_ANALYZER_ANNOTATIONS` symbol is defined. Use the assembly-level form to exclude the methods of an assembly that does not define this symbol.

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

Use `CultureInsensitiveAttribute` to mark the value of a property, a field, or a parameter as culture-insensitive, even when its type is culture-sensitive. Methods cannot be annotated: the attribute marks a value, not the way a method computes it.

```csharp
class Sample
{
    [Meziantou.Analyzer.Annotations.CultureInsensitive]
    public static double InvariantValue => 0;
}

_ = $"{Sample.InvariantValue}"; // No MA0076 diagnostic
```

A parameter marked with the attribute is culture-insensitive in both directions: reading it in the method does not report a diagnostic, and MA0075/MA0076 are not reported for the arguments provided by the callers.

```csharp
static void Log([Meziantou.Analyzer.Annotations.CultureInsensitive] string message) { }

Log($"Value: {1.5}"); // No MA0076 diagnostic
```

Properties and fields of another assembly can be annotated at the assembly level using their XML documentation id.

```csharp
[assembly: Meziantou.Analyzer.Annotations.CultureInsensitiveAttribute("P:Sample.InvariantValue")]
```
