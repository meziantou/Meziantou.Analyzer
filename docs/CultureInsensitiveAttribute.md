# CultureInsensitiveAttribute

The `CultureInsensitiveAttribute` is used to mark the value of a member as culture-insensitive, even when the type of the value is culture-sensitive. This attribute can be used to suppress culture-related analyzer rules ([MA0011](Rules/MA0011.md), [MA0075](Rules/MA0075.md), [MA0076](Rules/MA0076.md)) for a specific method, property, field, or parameter.

Use [CultureInsensitiveTypeAttribute](CultureInsensitiveTypeAttribute.md) when a whole type is culture-insensitive, and `CultureInsensitiveAttribute` when only some values of a culture-sensitive type are known to be culture-insensitive.

## Usage

The attribute is available through the [`Meziantou.Analyzer.Annotations`](https://www.nuget.org/packages/Meziantou.Analyzer.Annotations/) NuGet package.

Alternatively, you can define the attribute in your own assembly instead of using the package. The analyzer only looks for the attribute by name and namespace, so you can copy the [attribute definition](https://github.com/meziantou/Meziantou.Analyzer/blob/main/src/Meziantou.Analyzer.Annotations/CultureInsensitiveAttribute.cs) into your project.

### Marking the Return Value of a Method

```csharp
using Meziantou.Analyzer.Annotations;

class Sample
{
    [CultureInsensitive] // Equivalent to [return: CultureInsensitive]
    public static double GetInvariantValue() => 0;

    public static double GetValue() => 0;
}

// Usage
_ = $"{Sample.GetInvariantValue()}";     // OK - The returned value is marked as culture-insensitive
_ = "value: " + Sample.GetValue();       // Warning - MA0075
```

### Marking a Property or a Field

```csharp
using Meziantou.Analyzer.Annotations;

class Sample
{
    [CultureInsensitive]
    public double Value { get; set; }

    [CultureInsensitive]
    public double Field;
}

// Usage - no warning
_ = $"{sample.Value} {sample.Field}";
```

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

When you cannot modify the source member (e.g., third-party libraries), use the assembly-level attribute with the [XML documentation id](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/#id-strings) of the member:

```csharp
using Meziantou.Analyzer.Annotations;

// Mark a single overload as culture-insensitive
[assembly: CultureInsensitive("M:Sample.StringHelper.CreateInvariant(System.Double)")]

// Omit the parameter list to mark all the overloads of a method
[assembly: CultureInsensitive("M:Sample.StringHelper.CreateInvariant")]

// Properties and fields are also supported
[assembly: CultureInsensitive("P:Sample.StringHelper.InvariantValue")]
```

## Constructors

| Constructor | Description |
|-------------|-------------|
| `CultureInsensitive()` | Marks the value of the member on which the attribute is applied as culture-insensitive |
| `CultureInsensitive(string xmlDocumentationId)` | Assembly-level: marks the value of the member matching the XML documentation id as culture-insensitive |

## Related Rules

- [MA0011](Rules/MA0011.md) - IFormatProvider is missing
- [MA0075](Rules/MA0075.md) - Do not use implicit culture-sensitive ToString
- [MA0076](Rules/MA0076.md) - Do not use implicit culture-sensitive ToString in interpolated strings
- [MA0185](Rules/MA0185.md) - Simplify string.Create when all parameters are culture invariant

## Additional Information

The attribute is marked with `[Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]`, which means it is only compiled into your assembly when the `MEZIANTOU_ANALYZER_ANNOTATIONS` compilation symbol is defined. This keeps the attribute metadata in your assembly for use by analyzers without affecting runtime behavior.
