using System.Linq;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer;

internal static class MethodSymbolExtensions
{
    private static readonly string[] s_msTestNamespaceParts = ["Microsoft", "VisualStudio", "TestTools", "UnitTesting"];
    private static readonly string[] s_nunitNamespaceParts = ["NUnit", "Framework"];
    private static readonly string[] s_xunitNamespaceParts = ["Xunit"];

    public static bool IsInterfaceImplementation(this IMethodSymbol method)
    {
        if (method.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return IsInterfaceImplementation((ISymbol)method);
    }

    public static bool IsInterfaceImplementation(this IPropertySymbol property)
    {
        if (property.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return IsInterfaceImplementation((ISymbol)property);
    }

    public static bool IsInterfaceImplementation(this IEventSymbol method)
    {
        if (method.ExplicitInterfaceImplementations.Length > 0)
            return true;

        return IsInterfaceImplementation((ISymbol)method);
    }

    private static bool IsInterfaceImplementation(this ISymbol symbol)
    {
        return GetImplementingInterfaceSymbol(symbol) != null;
    }

    public static IMethodSymbol? GetImplementingInterfaceSymbol(this IMethodSymbol symbol)
    {
        if (symbol.ExplicitInterfaceImplementations.Any())
            return symbol.ExplicitInterfaceImplementations.First();

        return (IMethodSymbol?)GetImplementingInterfaceSymbol((ISymbol)symbol);
    }

    private static ISymbol? GetImplementingInterfaceSymbol(this ISymbol symbol)
    {
        if (symbol.ContainingType == null)
            return null;

        return symbol.ContainingType.AllInterfaces
            .SelectMany(@interface => @interface.GetMembers())
            .FirstOrDefault(interfaceMember => SymbolEqualityComparer.Default.Equals(symbol, symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember)));
    }

    public static bool IsUnitTestMethod(this IMethodSymbol methodSymbol)
    {
        var attributes = methodSymbol.GetAttributes();
        foreach (var attribute in attributes)
        {
            var type = attribute.AttributeClass;
            while (type != null)
            {
                var ns = type.ContainingNamespace;
                if (ns.IsNamespace(s_msTestNamespaceParts) ||
                    ns.IsNamespace(s_nunitNamespaceParts) ||
                    ns.IsNamespace(s_xunitNamespaceParts))
                {
                    return true;
                }

                type = type.BaseType;
            }
        }

        return false;
    }
}
