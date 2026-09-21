using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Formats the values of the attributes that expose a symbol or a constant. The formats are the ones Roslyn
/// produces, so they are predictable and documented elsewhere.
/// </summary>
internal static class XPathAttributeFormatter
{
    private static readonly ConcurrentDictionary<SpecialType, string> SpecialTypeNames = new();
    private static readonly ConcurrentDictionary<SymbolKind, string> SymbolKindNames = new();

    /// <summary>
    /// Adds the attributes of a type. The metadata name and the documentation comment id have no type arguments, so
    /// they are the ones of the definition of the type. The reference id is the only format that carries them.
    /// </summary>
    public static void AddType(in XPathAttributeWriter writer, TextSpan span, ITypeSymbol? type, TypeAttributeNames names)
    {
        if (type is null)
            return;

        writer.Add(span, names.Name, type.Name);

        // The formatted names are the expensive ones, so they are only built when a query can select them
        if (writer.Includes(names.MetadataName))
        {
            writer.Add(span, names.MetadataName, SymbolNameFormatter.GetMetadataName(type));
        }

        if (writer.Includes(names.DocumentationId))
        {
            writer.Add(span, names.DocumentationId, SymbolNameFormatter.GetDocumentationId(type));
        }

        if (writer.Includes(names.ReferenceId))
        {
            writer.Add(span, names.ReferenceId, SymbolNameFormatter.GetReferenceId(type));
        }

        writer.Add(span, names.IsValueType, ToXPathBoolean(type.IsValueType));
        writer.Add(span, names.NullableAnnotation, GetNullableAnnotationName(type.NullableAnnotation));
        writer.Add(span, names.SpecialType, GetSpecialTypeName(type.SpecialType));
    }

    /// <summary>
    /// Adds the attributes of a symbol that is not a type.
    /// </summary>
    public static void AddSymbol(in XPathAttributeWriter writer, TextSpan span, ISymbol? symbol, SymbolAttributeNames names)
    {
        if (symbol is null)
            return;

        if (writer.Includes(names.QualifiedName))
        {
            writer.Add(span, names.QualifiedName, SymbolNameFormatter.GetSymbolName(symbol));
        }

        writer.Add(span, names.Name, symbol.Name);

        if (writer.Includes(names.DocumentationId))
        {
            writer.Add(span, names.DocumentationId, SymbolNameFormatter.GetDocumentationId(symbol));
        }

        writer.Add(span, names.Kind, GetSymbolKindName(symbol.Kind));
        writer.Add(span, names.IsStatic, ToXPathBoolean(symbol.IsStatic));
    }

    // XPath 1.0 has no boolean value in an attribute, so the value is the one the language uses
    public static string ToXPathBoolean(bool value) => value ? "true" : "false";

    public static string GetSymbolKindName(SymbolKind kind) => SymbolKindNames.GetOrAdd(kind, static kind => kind.ToString());

    // The accessibility is not exposed when it does not apply to the symbol, such as for a local or a parameter
    public static string? GetAccessibilityName(Accessibility accessibility) => accessibility switch
    {
        Accessibility.Private => nameof(Accessibility.Private),
        Accessibility.ProtectedAndInternal => nameof(Accessibility.ProtectedAndInternal),
        Accessibility.Protected => nameof(Accessibility.Protected),
        Accessibility.Internal => nameof(Accessibility.Internal),
        Accessibility.ProtectedOrInternal => nameof(Accessibility.ProtectedOrInternal),
        Accessibility.Public => nameof(Accessibility.Public),
        _ => null,
    };

    // The annotation is 'None' in a file where the nullable context is disabled, which is not exposed
    public static string? GetNullableAnnotationName(NullableAnnotation annotation) => annotation switch
    {
        NullableAnnotation.NotAnnotated => nameof(NullableAnnotation.NotAnnotated),
        NullableAnnotation.Annotated => nameof(NullableAnnotation.Annotated),
        _ => null,
    };

    public static string? GetSpecialTypeName(SpecialType specialType)
    {
        if (specialType is SpecialType.None)
            return null;

        return SpecialTypeNames.GetOrAdd(specialType, static specialType => specialType.ToString());
    }

    public static string? FormatConstantValue(object? value) => value switch
    {
        null => null,
        bool boolean => boolean ? "true" : "false",
        string text => text,
        char character => character.ToString(),
        IFormattable formattable => formattable.ToString(format: null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };
}
