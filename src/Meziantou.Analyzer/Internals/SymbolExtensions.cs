using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer
{
    internal static class SymbolExtensions
    {
        public static bool IsVisible(this ISymbol symbol)
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

            return IsVisible(symbol.ContainingType);
        }
    }
}
