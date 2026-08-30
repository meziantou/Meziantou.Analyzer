namespace Meziantou.Analyzer.Internals;

internal static class AnnotationExclusions
{
    /// <summary>
    /// Resolves the symbols excluded by the assembly-level annotation attributes matching <paramref name="isAttributeSymbol"/>.
    /// The attributes can use an XML documentation id, a containing type and a member name, or a containing type, a member name and the parameter types.
    /// </summary>
    public static HashSet<ISymbol> GetExcludedSymbols(Compilation compilation, Func<ITypeSymbol?, bool> isAttributeSymbol)
    {
        var result = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!isAttributeSymbol(attribute.AttributeClass))
                continue;

            var constructorArguments = attribute.ConstructorArguments;
            if (constructorArguments is [{ Type.SpecialType: SpecialType.System_String, IsNull: false, Value: string documentationId }])
            {
                foreach (var symbol in DocumentationCommentId.GetSymbolsForDeclarationId(documentationId, compilation))
                {
                    AddExcludedSymbol(result, symbol);
                }

                continue;
            }

            if (constructorArguments.Length is not 2 and not 3)
                continue;

            if (constructorArguments[0].Value is not INamedTypeSymbol containingType)
                continue;

            if (constructorArguments[1] is not { Type.SpecialType: SpecialType.System_String, IsNull: false, Value: string memberName } || string.IsNullOrWhiteSpace(memberName))
                continue;

            if (constructorArguments.Length == 2)
            {
                foreach (var member in containingType.GetMembers(memberName))
                {
                    AddExcludedSymbol(result, member);
                }

                continue;
            }

            if (constructorArguments[2].Kind != TypedConstantKind.Array)
                continue;

            var parameterTypes = constructorArguments[2].Values;
            foreach (var method in containingType.GetMembers(memberName).OfType<IMethodSymbol>())
            {
                if (method.Parameters.Length != parameterTypes.Length)
                    continue;

                var match = true;
                for (var i = 0; i < parameterTypes.Length; i++)
                {
                    if (parameterTypes[i].Value is not ITypeSymbol parameterType || !method.Parameters[i].Type.IsEqualTo(parameterType))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    AddExcludedSymbol(result, method);
                }
            }
        }

        return result;

        static void AddExcludedSymbol(HashSet<ISymbol> symbols, ISymbol symbol)
        {
            if (symbol is not IMethodSymbol and not IPropertySymbol)
                return;

            symbols.Add(symbol);
            if (!ReferenceEquals(symbol.OriginalDefinition, symbol))
            {
                symbols.Add(symbol.OriginalDefinition);
            }
        }
    }
}
