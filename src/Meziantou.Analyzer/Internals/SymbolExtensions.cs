using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Internals;

internal static class SymbolExtensions
{
    public static bool IsEqualTo(this ISymbol? symbol, [NotNullWhen(true)] ISymbol? expectedType)
    {
        if (symbol is null || expectedType is null)
            return false;

        return SymbolEqualityComparer.Default.Equals(expectedType, symbol);
    }

    public static bool IsOperator(this ISymbol? symbol)
    {
        return symbol is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion };
    }

    public static bool IsConst(this ISymbol? symbol)
    {
        return symbol is IFieldSymbol field && field.IsConst;
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

    public static IEnumerable<ISymbol> GetAllMembers(this INamespaceOrTypeSymbol? symbol, string name)
    {
        while (symbol is not null)
        {
            foreach (var member in symbol.GetMembers(name))
                yield return member;

            if (symbol is INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceSymbol)
            {
                foreach (var iface in interfaceSymbol.AllInterfaces)
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
            {
                // ASP.NET Core generates a public partial class for top-level statements. We need to skip thoses.
                if (syntax.SyntaxTree.FilePath?.EndsWith(".g.cs", StringComparison.Ordinal) is true)
                    continue;

                return false;
            }
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
}
