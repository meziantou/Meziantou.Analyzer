# CultureInsensitiveAttribute

The `CultureInsensitiveAttribute` is used to mark the value of a property, a field, or a parameter as culture-insensitive, even when the type of the value is culture-sensitive. This attribute can be used to suppress culture-related analyzer rules ([MA0011](Rules/MA0011.md), [MA0075](Rules/MA0075.md), [MA0076](Rules/MA0076.md)) for that value.

Use [CultureInsensitiveTypeAttribute](CultureInsensitiveTypeAttribute.md) when a whole type is culture-insensitive, and `CultureInsensitiveAttribute` when only some values of a culture-sensitive type are known to be culture-insensitive.

## Usage

The attribute is available through the [`Meziantou.Analyzer.Annotations`](https://www.nuget.org/packages/Meziantou.Analyzer.Annotations/) NuGet package.

Alternatively, you can define the attribute in your own assembly instead of using the package. The analyzer only looks for the attribute by name and namespace, so you can copy the [attribute definition](https://github.com/meziantou/Meziantou.Analyzer/blob/main/src/Meziantou.Analyzer.Annotations/CultureInsensitiveAttribute.cs) into your project.

### Marking a Property or a Field

```csharp
using Meziantou.Analyzer.Annotations;

class Sample
{
    [CultureInsensitive]
    public double Value { get; set; }

    [CultureInsensitive]
    public double Field;

    public double OtherValue { get; set; }
}

// Usage
_ = $"{sample.Value} {sample.Field}"; // OK - Both values are marked as culture-insensitive
_ = "value: " + sample.OtherValue;    // Warning - MA0075
```

Methods cannot be annotated: the attribute marks a value, not the way a method computes it. When a method returns a culture-insensitive value of a culture-sensitive type, mark the type with [CultureInsensitiveTypeAttribute](CultureInsensitiveTypeAttribute.md), or assign the result to an annotated property or field.

### Marking a Parameter

A parameter marked with the attribute is culture-insensitive in both directions: reading it in the method does not report a diagnostic, and [MA0075](Rules/MA0075.md) and [MA0076](Rules/MA0076.md) are not reported for the arguments provided by the callers. The latter is useful for a method that formats its argument with a fixed culture, such as a wrapper around an interpolated string handler.

```csharp
using Meziantou.Analyzer.Annotations;

class Sample
{
    public static void Write([CultureInsensitive] double value)
    {
        _ = $"{value}"; // OK - The parameter is marked as culture-insensitive
    }

    public static void Log([CultureInsensitive] string message) { }

    public static string Format(string message) => message;
}

// Usage
Sample.Log($"Value: {1.5}");         // OK - The argument is passed to a culture-insensitive parameter
Sample.Log(Sample.Format($"{1.5}")); // Warning - The interpolated string is an argument of Format
```

Only the closest argument is considered, so a value nested in another invocation is still reported.

### Assembly-Level Annotation for External Members

When you cannot modify the source member (e.g., third-party libraries), use the assembly-level attribute with the [XML documentation id](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings) of the property or the field:

```csharp
using Meziantou.Analyzer.Annotations;

[assembly: CultureInsensitive("P:Sample.StringHelper.InvariantValue")]
[assembly: CultureInsensitive("F:Sample.StringHelper.InvariantField")]
```

## Constructors

| Constructor | Description |
|-------------|-------------|
| `CultureInsensitive()` | Marks the value of the property, the field, or the parameter on which the attribute is applied as culture-insensitive |
| `CultureInsensitive(string xmlDocumentationId)` | Assembly-level: marks the value of the property or the field matching the XML documentation id as culture-insensitive |

## Related Rules

- [MA0011](Rules/MA0011.md) - IFormatProvider is missing
- [MA0075](Rules/MA0075.md) - Do not use implicit culture-sensitive ToString
- [MA0076](Rules/MA0076.md) - Do not use implicit culture-sensitive ToString in interpolated strings
- [MA0185](Rules/MA0185.md) - Simplify string.Create when all parameters are culture invariant

## Additional Information

The attribute is marked with `[Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]`, which means it is only compiled into your assembly when the `MEZIANTOU_ANALYZER_ANNOTATIONS` compilation symbol is defined. This keeps the attribute metadata in your assembly for use by analyzers without affecting runtime behavior.
