using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;

// The attribute can be defined in multiple assemblies, so it's identified by its full name only
internal static class AnnotationAttributes
{
    public static bool IsCultureSensitiveAttributeSymbol(ITypeSymbol? symbol)
    {
        // Meziantou.Analyzer.Annotations.CultureInsensitiveTypeAttribute        
        return symbol is INamedTypeSymbol
        {
            Name: "CultureInsensitiveTypeAttribute",
            ContainingSymbol: INamespaceSymbol
            {
                Name: "Annotations",
                ContainingSymbol: INamespaceSymbol
                {
                    Name: "Analyzer",
                    ContainingSymbol: INamespaceSymbol
                    {
                        Name: "Meziantou",
                        ContainingSymbol: INamespaceSymbol { IsGlobalNamespace: true }
                    }
                }
            }
        };
    }

    public static bool IsRequireNamedArgumentAttributeSymbol(ITypeSymbol? symbol)
    {
        // Meziantou.Analyzer.Annotations.RequireNamedArgumentAttribute        
        return symbol is INamedTypeSymbol
        {
            Name: "RequireNamedArgumentAttribute",
            ContainingSymbol: INamespaceSymbol
            {
                Name: "Annotations",
                ContainingSymbol: INamespaceSymbol
                {
                    Name: "Analyzer",
                    ContainingSymbol: INamespaceSymbol
                    {
                        Name: "Meziantou",
                        ContainingSymbol: INamespaceSymbol { IsGlobalNamespace: true }
                    }
                }
            }
        };
    }
}
