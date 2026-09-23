using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

internal static class InheritdocOnTypesCommon
{
    /// <summary>
    /// Gets the interfaces the type inherits the documentation from, ignoring the <c>IEquatable&lt;T&gt;</c> interface the compiler implements on the records.
    /// </summary>
    public static ImmutableArray<INamedTypeSymbol> GetInterfaces(INamedTypeSymbol symbol, CancellationToken cancellationToken)
    {
        var interfaces = symbol.Interfaces;
        if (!symbol.IsRecord)
            return interfaces;

        for (var i = 0; i < interfaces.Length; i++)
        {
            if (IsRecordEquatableInterface(symbol, interfaces[i]))
            {
                if (IsEquatableInterfaceDeclared(symbol, cancellationToken))
                    return interfaces;

                return interfaces.RemoveAt(i);
            }
        }

        return interfaces;
    }

    private static bool IsRecordEquatableInterface(INamedTypeSymbol symbol, INamedTypeSymbol interfaceSymbol)
    {
        return interfaceSymbol is { Name: "IEquatable", Arity: 1, ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } }
            && SymbolEqualityComparer.Default.Equals(interfaceSymbol.TypeArguments[0], symbol);
    }

    private static bool IsEquatableInterfaceDeclared(INamedTypeSymbol symbol, CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax { BaseList: { } baseList })
                continue;

            foreach (var baseType in baseList.Types)
            {
                var name = baseType.Type switch
                {
                    QualifiedNameSyntax qualifiedName => qualifiedName.Right,
                    AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name,
                    SimpleNameSyntax simpleName => simpleName,
                    _ => null,
                };

                if (name is GenericNameSyntax { Identifier.ValueText: "IEquatable", TypeArgumentList.Arguments.Count: 1 })
                    return true;
            }
        }

        return false;
    }
}
