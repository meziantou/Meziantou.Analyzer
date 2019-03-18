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

            return method.ContainingType.AllInterfaces
                .SelectMany(@interface => @interface.GetMembers().OfType<IMethodSymbol>())
                .Any(interfaceMethod => method.Equals(method.ContainingType.FindImplementationForInterfaceMember(interfaceMethod)));
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
