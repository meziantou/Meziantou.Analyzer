using System.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Formats the name of a symbol for the <c>semantic</c> attributes of <see cref="SyntaxNodeXPathNavigator"/>.
/// </summary>
internal static class SymbolNameFormatter
{
    // Fully qualified, without 'global::' and without the C# keywords, so 'int?' is 'System.Nullable<System.Int32>'
    private static readonly SymbolDisplayFormat DisplayFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable);

    /// <summary>
    /// The fully qualified name of a type, with its type arguments: <c>System.Collections.Generic.List&lt;System.String&gt;</c>.
    /// </summary>
    public static string? GetDisplayName(ITypeSymbol? symbol) => symbol?.ToDisplayString(DisplayFormat);

    /// <summary>
    /// The name of a type as <see cref="Compilation.GetTypeByMetadataName(string)"/> expects it,
    /// with the arity suffix and without the type arguments: <c>System.Collections.Generic.List`1</c>.
    /// </summary>
    public static string? GetMetadataName(ITypeSymbol? symbol)
    {
        if (symbol is null)
            return null;

        var builder = new StringBuilder();
        AppendMetadataName(builder, symbol);
        return builder.ToString();
    }

    /// <summary>
    /// The name of a symbol, qualified by the metadata name of its containing type: <c>System.Console.WriteLine</c>.
    /// The parameters are not part of the name, so it is the name of all the overloads.
    /// </summary>
    public static string? GetSymbolName(ISymbol? symbol)
    {
        if (symbol is null)
            return null;

        symbol = symbol.OriginalDefinition;
        if (symbol is ITypeSymbol type)
            return GetMetadataName(type);

        if (symbol is INamespaceSymbol { IsGlobalNamespace: false } @namespace)
            return @namespace.ToDisplayString(DisplayFormat);

        var name = symbol.Name;
        if (name.Length is 0)
            return null;

        // A local, a parameter or a label is not a member of its containing type, so qualifying it would be misleading
        if (symbol.Kind is SymbolKind.Local or SymbolKind.Parameter or SymbolKind.RangeVariable or SymbolKind.Label or SymbolKind.TypeParameter)
            return name;

        var containingType = GetMetadataName(symbol.ContainingType);
        return containingType is null ? name : containingType + "." + name;
    }

    /// <summary>
    /// The documentation comment id of the definition of a symbol: <c>M:System.Console.WriteLine(System.String)</c>.
    /// </summary>
    public static string? GetDocumentationId(ISymbol? symbol)
    {
        if (symbol is null)
            return null;

        symbol = symbol.OriginalDefinition;

        // Only the symbols that can be documented have an id. The other ones, such as a parameter, a local or an
        // alias, throw in Roslyn 4.8 whereas the more recent versions return null.
        if (symbol.Kind is not (SymbolKind.Namespace or SymbolKind.NamedType or SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Event))
            return null;

        if (symbol is INamedTypeSymbol { IsAnonymousType: true } or IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction })
            return null;

        var id = DocumentationCommentId.CreateDeclarationId(symbol);
        if (id is null)
            return null;

        // CreateDeclarationId appends the return type of every method that does not return void, whereas the XML
        // documentation file only does it for the conversion operators. The id must be the one of the documentation,
        // so it can be copied from a documentation file or from a BannedSymbols.txt file.
        if (symbol is IMethodSymbol { MethodKind: not MethodKind.Conversion })
        {
            var separatorIndex = id.LastIndexOf("~", StringComparison.Ordinal);
            if (separatorIndex >= 0)
                return id.Substring(0, separatorIndex);
        }

        return id;
    }

    // ToDisplayString cannot emit the arity suffix of a generic type nor the '+' that separates the nested types,
    // so the metadata name is built from the metadata name of each part
    private static void AppendMetadataName(StringBuilder builder, ITypeSymbol symbol)
    {
        switch (symbol)
        {
            case IArrayTypeSymbol array:
                AppendMetadataName(builder, array.ElementType);
                builder.Append('[').Append(',', array.Rank - 1).Append(']');
                break;

            case IPointerTypeSymbol pointer:
                AppendMetadataName(builder, pointer.PointedAtType);
                builder.Append('*');
                break;

            case INamedTypeSymbol { IsAnonymousType: false } namedType:
                if (namedType.ContainingType is { } containingType)
                {
                    AppendMetadataName(builder, containingType);
                    builder.Append('+');
                }
                else if (namedType.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace)
                {
                    builder.Append(containingNamespace.ToDisplayString(DisplayFormat)).Append('.');
                }

                builder.Append(namedType.MetadataName);
                break;

            default:
                builder.Append(symbol.ToDisplayString(DisplayFormat));
                break;
        }
    }
}
