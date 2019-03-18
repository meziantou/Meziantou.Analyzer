using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer
{
    internal static class NamespaceSymbolExtensions
    {

        public static bool IsNamespace(this INamespaceSymbol namespaceSymbol, string[] ns)
        {
            for (var i = ns.Length - 1; i >= 0; i--)
            {
                if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
                    return false;

                if (!string.Equals(ns[i], namespaceSymbol.Name, System.StringComparison.Ordinal))
                    return false;

                namespaceSymbol = namespaceSymbol.ContainingNamespace;
            }

            return namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace;
        }
    }
}
