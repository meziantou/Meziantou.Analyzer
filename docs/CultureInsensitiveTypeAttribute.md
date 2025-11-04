# CultureInsensitiveTypeAttribute

The `CultureInsensitiveTypeAttribute` is used to mark types whose `ToString()` methods and format strings are culture-insensitive. This attribute can be used to suppress culture-related analyzer rules ([MA0011](Rules/MA0011.md), [MA0075](Rules/MA0075.md), [MA0076](Rules/MA0076.md)) for types that do not depend on the current culture.

## Usage

The attribute is available through the [`Meziantou.Analyzer.Annotations`](https://www.nuget.org/packages/Meziantou.Analyzer.Annotations/) NuGet package.

Alternatively, you can define the attribute in your own assembly instead of using the package. The analyzer only looks for the attribute by name and namespace, so you can copy the [attribute definition](https://github.com/meziantou/Meziantou.Analyzer/blob/main/src/Meziantou.Analyzer.Annotations/CultureInsensitiveTypeAttribute.cs) into your project.

### Marking a Type as Culture-Insensitive

Apply the attribute directly to a type to mark all its formats as culture-insensitive:

```csharp
using Meziantou.Analyzer.Annotations;

[CultureInsensitiveType]
public struct Ulid
{
    public override string ToString() => "..."; // Culture-insensitive implementation
}

// Usage - no warning
var id = new Ulid();
id.ToString(); // OK - Type is marked as culture-insensitive
```

### Marking Only the Default Format (ToString)

To mark only the parameterless `ToString()` method as culture-insensitive:

```csharp
[CultureInsensitiveType(isDefaultFormatCultureInsensitive: true)]
public struct MyType
{
    public override string ToString() => "..."; // Culture-insensitive
    public string ToString(string format, IFormatProvider provider) => "..."; // Still culture-sensitive
}

// Usage
var value = new MyType();
value.ToString(); // OK - Default format is marked as culture-insensitive
value.ToString("G", CultureInfo.InvariantCulture); // OK - Explicitly provides IFormatProvider
value.ToString("G", null); // Warning - Format with null provider is still culture-sensitive
```

### Marking a Specific Format

To mark only a specific format string as culture-insensitive:

```csharp
[CultureInsensitiveType("N")] // Only format "N" is culture-insensitive
public struct MyGuid
{
    public string ToString(string format) => format == "N" ? "..." : "...";
}

// Usage
var guid = new MyGuid();
guid.ToString("N"); // OK - Format "N" is culture-insensitive
guid.ToString("D"); // Warning - Format "D" is not marked as culture-insensitive
```

### Assembly-Level Annotation for External Types

When you cannot modify the source type (e.g., third-party libraries), use the assembly-level attribute:

```csharp
using Meziantou.Analyzer.Annotations;

// Mark all formats of Cysharp.Ulid as culture-insensitive
[assembly: CultureInsensitiveType(typeof(Cysharp.Ulid))]

// Or mark only the default ToString() method
[assembly: CultureInsensitiveType(typeof(Cysharp.Ulid), isDefaultFormatCultureInsensitive: true)]

// Or mark only a specific format
[assembly: CultureInsensitiveType(typeof(SomeType), "N")]
```

## Constructors

The attribute provides several constructors for different scenarios:

| Constructor | Description |
|-------------|-------------|
| `CultureInsensitiveType()` | Marks all formats of the type as culture-insensitive |
| `CultureInsensitiveType(string? format)` | Marks the specified format as culture-insensitive |
| `CultureInsensitiveType(bool isDefaultFormatCultureInsensitive)` | Marks only the default format (ToString()) as culture-insensitive when `true` |
| `CultureInsensitiveType(Type type)` | Assembly-level: marks all formats of the specified type as culture-insensitive |
| `CultureInsensitiveType(Type type, string? format)` | Assembly-level: marks the specified format of the type as culture-insensitive |
| `CultureInsensitiveType(Type type, bool isDefaultFormatCultureInsensitive)` | Assembly-level: marks only the default format of the type as culture-insensitive when `true` |

## Related Rules

- [MA0011](Rules/MA0011.md) - IFormatProvider is missing
- [MA0075](Rules/MA0075.md) - Do not use implicit culture-sensitive ToString
- [MA0076](Rules/MA0076.md) - Do not use implicit culture-sensitive ToString in interpolated strings

## Additional Information

The attribute is marked with `[Conditional("MEZIANTOU_ANALYZER_ANNOTATIONS")]`, which means it is only compiled into your assembly when the `MEZIANTOU_ANALYZER_ANNOTATIONS` compilation symbol is defined. This keeps the attribute metadata in your assembly for use by analyzers without affecting runtime behavior.
