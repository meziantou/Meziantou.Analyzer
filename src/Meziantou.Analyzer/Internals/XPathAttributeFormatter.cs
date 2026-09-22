using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.CodeAnalysis.Operations;
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
    private static readonly ConcurrentDictionary<TypeKind, string> TypeKindNames = new();

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

        writer.Add(span, names.Kind, GetTypeKindName(type.TypeKind));
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
        AddSymbolModifiers(writer, span, symbol, names);
    }

    /// <summary>
    /// Adds the attributes of the modifiers of a symbol. The modifiers that only a method or a type has are not added
    /// for the other symbols, so an attribute that is not present means that the modifier does not apply to the
    /// symbol, whereas the value <c>false</c> means that it applies and is not set.
    /// </summary>
    public static void AddSymbolModifiers(in XPathAttributeWriter writer, TextSpan span, ISymbol symbol, SymbolAttributeNames names)
    {
        writer.Add(span, names.IsStatic, ToXPathBoolean(symbol.IsStatic));
        writer.Add(span, names.IsAbstract, ToXPathBoolean(symbol.IsAbstract));
        writer.Add(span, names.IsVirtual, ToXPathBoolean(symbol.IsVirtual));
        writer.Add(span, names.IsOverride, ToXPathBoolean(symbol.IsOverride));
        writer.Add(span, names.IsSealed, ToXPathBoolean(symbol.IsSealed));

        switch (symbol)
        {
            case IMethodSymbol method:
                writer.Add(span, names.IsAsync, ToXPathBoolean(method.IsAsync));
                writer.Add(span, names.IsExtensionMethod, ToXPathBoolean(method.IsExtensionMethod));
                writer.Add(span, names.Arity, method.Arity.ToString(CultureInfo.InvariantCulture));
                break;

            case INamedTypeSymbol namedType:
                writer.Add(span, names.Arity, namedType.Arity.ToString(CultureInfo.InvariantCulture));
                break;
        }
    }

    /// <summary>
    /// Adds the attributes of a conversion. The conversion of an operation always has a value, so the attributes are
    /// present even when the conversion does not exist.
    /// </summary>
    public static void AddConversion(in XPathAttributeWriter writer, TextSpan span, CommonConversion conversion, ConversionAttributeNames names)
    {
        writer.Add(span, names.Exists, ToXPathBoolean(conversion.Exists));
        writer.Add(span, names.IsIdentity, ToXPathBoolean(conversion.IsIdentity));
        writer.Add(span, names.IsImplicit, ToXPathBoolean(conversion.IsImplicit));
        writer.Add(span, names.IsNullable, ToXPathBoolean(conversion.IsNullable));
        writer.Add(span, names.IsNumeric, ToXPathBoolean(conversion.IsNumeric));
        writer.Add(span, names.IsReference, ToXPathBoolean(conversion.IsReference));
        writer.Add(span, names.IsUserDefined, ToXPathBoolean(conversion.IsUserDefined));
        AddSymbol(writer, span, conversion.MethodSymbol, names.MethodNames);
    }

    // XPath 1.0 has no boolean value in an attribute, so the value is the one the language uses
    public static string ToXPathBoolean(bool value) => value ? "true" : "false";

    public static string GetSymbolKindName(SymbolKind kind) => SymbolKindNames.GetOrAdd(kind, static kind => kind.ToString());

    // The kind is not exposed when the type is not a real type, such as the type of an expression that does not compile
    public static string? GetTypeKindName(TypeKind kind)
    {
        if (kind is TypeKind.Unknown or TypeKind.Error)
            return null;

        return TypeKindNames.GetOrAdd(kind, static kind => kind.ToString());
    }

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
