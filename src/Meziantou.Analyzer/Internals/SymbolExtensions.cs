using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer
{
    internal static class SymbolExtensions
    {
        public static bool IsEqualTo(this ISymbol? symbol, ISymbol? expectedType)
        {
            if (symbol == null || expectedType == null)
                return false;

            return SymbolEqualityComparer.Default.Equals(expectedType, symbol);
        }

        public static bool IsVisibleOutsideOfAssembly([NotNullWhen(true)]this ISymbol? symbol)
        {
            if (symbol == null)
                return false;

            if (symbol.DeclaredAccessibility != Accessibility.Public &&
                symbol.DeclaredAccessibility != Accessibility.Protected &&
                symbol.DeclaredAccessibility != Accessibility.ProtectedOrInternal)
            {
                return false;
            }

            if (symbol.ContainingType == null)
                return true;

            return IsVisibleOutsideOfAssembly(symbol.ContainingType);
        }

        public static bool IsOperator(this ISymbol? symbol)
        {
            if (symbol is IMethodSymbol methodSymbol)
            {
                return methodSymbol.MethodKind == MethodKind.UserDefinedOperator || methodSymbol.MethodKind == MethodKind.Conversion;
            }

            return false;
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

        public static bool IsConst(this ISymbol? symbol)
        {
            return symbol is IFieldSymbol field && field.IsConst;
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol? symbol)
        {
            while (symbol != null)
            {
                foreach (var member in symbol.GetMembers())
                    yield return member;

                symbol = symbol.BaseType;
            }
        }
    }
}
