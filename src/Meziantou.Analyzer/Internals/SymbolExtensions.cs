using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;

internal static class SymbolExtensions
{
    public static bool IsEqualTo(this ISymbol? symbol, [NotNullWhen(true)] ISymbol? expectedType)
    {
        if (symbol is null || expectedType is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(expectedType, symbol);
    }

    public static bool IsVisibleOutsideOfAssembly([NotNullWhen(true)] this ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (symbol.DeclaredAccessibility is not Accessibility.Public and not Accessibility.Protected and not Accessibility.ProtectedOrInternal)
        {
            return false;
        }

        if (symbol.ContainingType is null)
            return true;

        return IsVisibleOutsideOfAssembly(symbol.ContainingType);
    }

    public static bool IsOperator(this ISymbol? symbol)
    {
        return symbol is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion };
    }

    public static bool IsOverrideOrInterfaceImplementation(this ISymbol? symbol)
    {
        if (symbol is IMethodSymbol methodSymbol)
            return methodSymbol.IsOverride || methodSymbol.IsInterfaceImplementation();

        if (symbol is IPropertySymbol propertySymbol)
            return propertySymbol.IsOverride || propertySymbol.IsInterfaceImplementation();

        if (symbol is IEventSymbol eventSymbol)
            return eventSymbol.IsOverride || eventSymbol.IsInterfaceImplementation();

        return false;
    }

    public static bool Override(this IMethodSymbol? symbol, ISymbol? baseSymbol)
    {
        if (baseSymbol is null)
            return false;

        var currentMethod = symbol?.OverriddenMethod;
        while (currentMethod is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseSymbol, currentMethod))
                return true;

            currentMethod = currentMethod.OverriddenMethod;
        }

        return false;
    }

    public static bool IsConst(this ISymbol? symbol)
    {
        return symbol is IFieldSymbol field && field.IsConst;
    }

    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? symbol)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers())
                yield return member;

            symbol = symbol.BaseType;
        }
    }

    public static IEnumerable<ISymbol> GetAllMembers(this INamespaceOrTypeSymbol? symbol)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers())
                yield return member;

            if (symbol is ITypeSymbol typeSymbol)
            {
                symbol = typeSymbol.BaseType;
            }
            else
            {
                yield break;
            }
        }
    }

    public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? symbol, string name)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers(name))
                yield return member;

            symbol = symbol.BaseType;
        }
    }

    public static IEnumerable<ISymbol> GetAllMembers(this INamespaceOrTypeSymbol? symbol, string name)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers(name))
                yield return member;

            if(symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceSymbol)
            {
                foreach(var iface in interfaceSymbol.AllInterfaces)
                {
                    foreach (var member in iface.GetMembers(name))
                        yield return member;
                }
            }

            if (symbol is ITypeSymbol typeSymbol)
            {
                symbol = typeSymbol.BaseType;
            }
            else
            {
                yield break;
            }
        }
    }

    public static bool IsTopLevelStatement(this ISymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.DeclaringSyntaxReferences.Length == 0)
            return false;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax(cancellationToken);
            if (!syntax.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.CompilationUnit))
                return false;
        }

        return true;
    }

    public static bool IsTopLevelStatementsEntryPointMethod([NotNullWhen(true)] this IMethodSymbol? methodSymbol)
    {
        return methodSymbol is { IsStatic: true, Name: "$Main" or "<Main>$" };
    }

    public static bool IsTopLevelStatementsEntryPointType([NotNullWhen(true)] this INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
            return false;

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.Kind is SymbolKind.Method)
            {
                var method = (IMethodSymbol)member;
                if (method.IsTopLevelStatementsEntryPointMethod())
                    return true;
            }
        }

        return false;
    }

    public static ITypeSymbol? GetSymbolType(this ISymbol symbol)
    {
        return symbol switch
        {
            IParameterSymbol parameter => parameter.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol { GetMethod: not null } property => property.Type,
            ILocalSymbol local => local.Type,
            IMethodSymbol method => method.ReturnType,
            _ => null,
        };
    }
}
