using System.Linq;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer
{
    internal static class MethodSymbolExtensions
    {
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


        private static bool IsInterfaceImplementation(this ISymbol symbol)
        {
            if (symbol.ContainingType == null)
                return false;

            return symbol.ContainingType.AllInterfaces
                .SelectMany(@interface => @interface.GetMembers())
                .Any(interfaceMember => symbol.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember)));
        }

        public static bool IsUnitTestMethod(this IMethodSymbol methodSymbol)
        {
            var attributes = methodSymbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                var ns = attribute.AttributeClass.ContainingNamespace;
                if (ns.IsNamespace(new[] { "Microsoft", "VisualStudio", "TestTools", "UnitTesting" }) ||
                    ns.IsNamespace(new[] { "NUnit", "Framework" }) ||
                    ns.IsNamespace(new[] { "Xunit" }))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
