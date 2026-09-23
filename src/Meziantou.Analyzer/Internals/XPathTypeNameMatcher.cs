namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Matches a type against the name a query gives to the <c>implements</c>, <c>inherits-from</c>,
/// <c>is-assignable-to</c> and <c>has-attribute</c> functions. The name is in one of the formats of the attributes
/// that expose a type, so it can be copied from one of them.
/// </summary>
internal sealed class XPathTypeNameMatcher
{
    /// <summary>The name of the format of <c>System.Collections.Generic.List`1</c>.</summary>
    public const string MetadataNameFormat = "MetadataName";

    /// <summary>The name of the format of <c>T:System.Collections.Generic.List`1</c>.</summary>
    public const string DocumentationDeclarationIdFormat = "DocumentationDeclarationId";

    /// <summary>The name of the format of <c>System.Collections.Generic.List{System.String}</c>.</summary>
    public const string DocumentationReferenceIdFormat = "DocumentationReferenceId";

    private readonly string _name;
    private readonly TypeNameFormat _format;

    // The name of the type alone, which is compared before formatting the full name of a type, as most of the types
    // a query visits do not match
    private readonly string _simpleName;

    private XPathTypeNameMatcher(string name, TypeNameFormat format)
    {
        _name = name;
        _format = format;
        _simpleName = GetSimpleName(name, format);
    }

    /// <summary>
    /// Indicates whether a value is the name of a format.
    /// </summary>
    public static bool IsFormatName(string value) => TryParseFormat(value, out _);

    /// <summary>
    /// Creates a matcher for a name. The format is detected from the name when it is not given. It is null when the
    /// format is not valid, so the name matches nothing.
    /// </summary>
    public static XPathTypeNameMatcher? Create(string name, string? format)
    {
        if (format is null)
            return new XPathTypeNameMatcher(name, DetectFormat(name));

        return TryParseFormat(format, out var parsedFormat) ? new XPathTypeNameMatcher(name, parsedFormat) : null;
    }

    public bool Matches(ITypeSymbol type)
    {
        // The name of an array or of a pointer is empty, so it is not compared first
        if (type is INamedTypeSymbol namedType && !string.Equals(_format is TypeNameFormat.DocumentationReferenceId ? namedType.Name : namedType.MetadataName, _simpleName, StringComparison.Ordinal))
            return false;

        var name = _format switch
        {
            TypeNameFormat.MetadataName => SymbolNameFormatter.GetMetadataName(type.OriginalDefinition),
            TypeNameFormat.DocumentationDeclarationId => SymbolNameFormatter.GetDocumentationId(type),
            _ => SymbolNameFormatter.GetReferenceId(type),
        };

        return string.Equals(name, _name, StringComparison.Ordinal);
    }

    // A documentation comment id starts with the kind of the symbol, such as 'T:', and a reference id is the only
    // format that has type arguments
    private static TypeNameFormat DetectFormat(string name)
    {
        if (name.Length >= 2 && char.IsLetter(name[0]) && name[1] is ':')
            return TypeNameFormat.DocumentationDeclarationId;

        if (name.Contains('{', StringComparison.Ordinal))
            return TypeNameFormat.DocumentationReferenceId;

        return TypeNameFormat.MetadataName;
    }

    private static bool TryParseFormat(string value, out TypeNameFormat format)
    {
        switch (value)
        {
            case MetadataNameFormat:
                format = TypeNameFormat.MetadataName;
                return true;

            case DocumentationDeclarationIdFormat:
                format = TypeNameFormat.DocumentationDeclarationId;
                return true;

            case DocumentationReferenceIdFormat:
                format = TypeNameFormat.DocumentationReferenceId;
                return true;

            default:
                format = default;
                return false;
        }
    }

    // The metadata name of a type is the last part of its metadata name and of its documentation comment id, such as
    // 'List`1', whereas the reference id has the name of the type without the arity and followed by its type
    // arguments, such as 'List{System.String}', which can contain '.' too
    private static string GetSimpleName(string name, TypeNameFormat format)
    {
        var start = 0;
        var end = name.Length;
        var depth = 0;
        for (var i = 0; i < name.Length; i++)
        {
            switch (name[i])
            {
                case '{' when format is TypeNameFormat.DocumentationReferenceId:
                    if (depth is 0)
                    {
                        end = i;
                    }

                    depth++;
                    break;

                case '}' when format is TypeNameFormat.DocumentationReferenceId:
                    depth--;
                    break;

                case '.' or '+' or ':' when depth is 0:
                    start = i + 1;
                    end = name.Length;
                    break;
            }
        }

        return end > start ? name.Substring(start, end - start) : "";
    }

    private enum TypeNameFormat
    {
        MetadataName,
        DocumentationDeclarationId,
        DocumentationReferenceId,
    }
}
