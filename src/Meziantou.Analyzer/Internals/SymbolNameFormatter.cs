using System.Globalization;
using System.Text;

namespace Meziantou.Analyzer.Internals;

/// <summary>
/// Formats the name of a symbol for the <c>semantic</c> attributes of <see cref="SyntaxNodeXPathNavigator"/>.
/// <see cref="ISymbol.ToDisplayString(SymbolDisplayFormat)"/> cannot emit the arity suffix of the metadata names,
/// so the names are built here.
/// </summary>
internal static class SymbolNameFormatter
{
    /// <summary>
    /// The fully qualified name of a type, with its type arguments: <c>System.Collections.Generic.List&lt;System.String&gt;</c>.
    /// </summary>
    public static string? GetDisplayName(ITypeSymbol? symbol) => Format(symbol, metadata: false);

    /// <summary>
    /// The name of a type as <see cref="Compilation.GetTypeByMetadataName(string)"/> expects it,
    /// with the arity suffix and without the type arguments: <c>System.Collections.Generic.List`1</c>.
    /// </summary>
    public static string? GetMetadataName(ITypeSymbol? symbol) => Format(symbol, metadata: true);

    /// <summary>
    /// The name of a symbol, qualified by the metadata name of its containing type: <c>System.Console.WriteLine</c>.
    /// The parameters are not part of the name, so the name of an overload is the name of all of them.
    /// </summary>
    public static string? GetSymbolName(ISymbol? symbol)
    {
        if (symbol is null)
            return null;

        symbol = symbol.OriginalDefinition;
        if (symbol is ITypeSymbol type)
            return GetMetadataName(type);

        if (symbol is INamespaceSymbol ns)
            return GetNamespaceName(ns);

        var name = symbol.Name;
        if (string.IsNullOrEmpty(name))
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

    private static string? Format(ITypeSymbol? symbol, bool metadata)
    {
        if (symbol is null)
            return null;

        var builder = new StringBuilder();
        AppendType(builder, symbol, metadata);
        return builder.Length is 0 ? null : builder.ToString();
    }

    private static void AppendType(StringBuilder builder, ITypeSymbol symbol, bool metadata)
    {
        switch (symbol)
        {
            case IArrayTypeSymbol array:
                AppendType(builder, array.ElementType, metadata);
                builder.Append('[').Append(',', array.Rank - 1).Append(']');
                break;

            case IPointerTypeSymbol pointer:
                AppendType(builder, pointer.PointedAtType, metadata);
                builder.Append('*');
                break;

            case ITypeParameterSymbol typeParameter:
                builder.Append(typeParameter.Name);
                break;

            case IDynamicTypeSymbol:
                builder.Append("dynamic");
                break;

            case INamedTypeSymbol namedType:
                AppendNamedType(builder, namedType, metadata);
                break;

            default:
                builder.Append(symbol.ToDisplayString());
                break;
        }
    }

    private static void AppendNamedType(StringBuilder builder, INamedTypeSymbol symbol, bool metadata)
    {
        // A tuple is a ValueTuple<...>, which is what a query can be written against
        if (symbol.IsTupleType && symbol.TupleUnderlyingType is { } underlyingType)
        {
            symbol = underlyingType;
        }

        if (symbol.IsAnonymousType)
        {
            builder.Append(symbol.ToDisplayString());
            return;
        }

        if (symbol.ContainingType is { } containingType)
        {
            AppendNamedType(builder, containingType, metadata);
            builder.Append(metadata ? '+' : '.');
        }
        else
        {
            AppendNamespace(builder, symbol.ContainingNamespace);
        }

        builder.Append(symbol.Name);
        if (symbol.Arity is 0)
            return;

        if (metadata)
        {
            builder.Append('`').Append(symbol.Arity.ToString(CultureInfo.InvariantCulture));
            return;
        }

        builder.Append('<');
        for (var i = 0; i < symbol.TypeArguments.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            AppendType(builder, symbol.TypeArguments[i], metadata);
        }

        builder.Append('>');
    }

    private static string? GetNamespaceName(INamespaceSymbol symbol)
    {
        if (symbol.IsGlobalNamespace)
            return null;

        var builder = new StringBuilder();
        AppendNamespace(builder, symbol.ContainingNamespace);
        builder.Append(symbol.Name);
        return builder.ToString();
    }

    private static void AppendNamespace(StringBuilder builder, INamespaceSymbol? symbol)
    {
        if (symbol is null || symbol.IsGlobalNamespace)
            return;

        AppendNamespace(builder, symbol.ContainingNamespace);
        builder.Append(symbol.Name).Append('.');
    }
}
