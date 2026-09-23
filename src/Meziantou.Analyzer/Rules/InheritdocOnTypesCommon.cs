using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules;

internal static class InheritdocOnTypesCommon
{
    /// <summary>
    /// Gets the interfaces the type inherits the documentation from, ignoring the <c>IEquatable&lt;T&gt;</c> interface the compiler implements on the records.
    /// </summary>
    public static ImmutableArray<INamedTypeSymbol> GetInterfaces(INamedTypeSymbol symbol, Compilation compilation, CancellationToken cancellationToken)
    {
        var interfaces = symbol.Interfaces;
        if (!symbol.IsRecord)
            return interfaces;

        for (var i = 0; i < interfaces.Length; i++)
        {
            if (IsRecordEquatableInterface(symbol, interfaces[i]))
            {
                if (IsInterfaceDeclared(symbol, interfaces[i], compilation, cancellationToken))
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

    /// <summary>
    /// Determines whether the interface is written in the base list of the type, as the compiler merges an explicitly declared <c>IEquatable&lt;T&gt;</c>
    /// with the one it implements on the records.
    /// </summary>
    private static bool IsInterfaceDeclared(INamedTypeSymbol symbol, INamedTypeSymbol interfaceSymbol, Compilation compilation, CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not TypeDeclarationSyntax { BaseList: { } baseList })
                continue;

            foreach (var baseType in baseList.Types)
            {
                // Only bind the base types that can be the interface
                var name = baseType.Type switch
                {
                    QualifiedNameSyntax qualifiedName => qualifiedName.Right,
                    AliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName.Name,
                    SimpleNameSyntax simpleName => simpleName,
                    _ => null,
                };

                if (name is not GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } genericName || genericName.Identifier.ValueText != interfaceSymbol.Name)
                    continue;

                var semanticModel = compilation.GetSemanticModel(baseType.SyntaxTree);
                if (SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(baseType.Type, cancellationToken).Type, interfaceSymbol))
                    return true;
            }
        }

        return false;
    }
}
