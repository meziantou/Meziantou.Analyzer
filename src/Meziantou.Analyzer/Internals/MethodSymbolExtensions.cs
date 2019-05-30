using System.Linq;
using System.Reflection;
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

        public static IMethodSymbol GetImplementingInterfaceSymbol(this IMethodSymbol symbol)
        {
            if (symbol.ExplicitInterfaceImplementations.Any())
                return symbol.ExplicitInterfaceImplementations.First();

            return (IMethodSymbol)GetImplementingInterfaceSymbol((ISymbol)symbol);
        }

        private static ISymbol GetImplementingInterfaceSymbol(this ISymbol symbol)
        {
            if (symbol.ContainingType == null)
                return null;

            return symbol.ContainingType.AllInterfaces
                .SelectMany(@interface => @interface.GetMembers())
                .FirstOrDefault(interfaceMember => symbol.Equals(symbol.ContainingType.FindImplementationForInterfaceMember(interfaceMember)));
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

        internal static bool HasOverloadWithAdditionalParameterOfType(
            this IMethodSymbol methodSymbol,
            Compilation compilation,
            params ITypeSymbol[] additionalParameterTypes)
        {
            return FindOverloadWithAdditionalParameterOfType(methodSymbol, compilation, additionalParameterTypes) != null;
        }

        internal static IMethodSymbol FindOverloadWithAdditionalParameterOfType(
            this IMethodSymbol methodSymbol,
            Compilation compilation,
            params ITypeSymbol[] additionalParameterTypes)
        {
            return FindOverloadWithAdditionalParameterOfType(methodSymbol, compilation, includeObsoleteMethods: false, additionalParameterTypes);
        }

        internal static IMethodSymbol FindOverloadWithAdditionalParameterOfType(
            this IMethodSymbol methodSymbol,
            Compilation compilation,
            bool includeObsoleteMethods,
            params ITypeSymbol[] additionalParameterTypes)
        {
            var obsoleteAttribute = compilation?.GetTypeByMetadataName("System.ObsoleteAttribute");

            var members = methodSymbol.ContainingType.GetMembers(methodSymbol.Name);
            return members.OfType<IMethodSymbol>().FirstOrDefault(IsOverload);

            bool IsOverload(IMethodSymbol member)
            {
                if (member.Equals(methodSymbol))
                    return false;

                if (member.Parameters.Length - additionalParameterTypes.Length != methodSymbol.Parameters.Length)
                    return false;

                if (!includeObsoleteMethods && member.HasAttribute(obsoleteAttribute))
                    return false;

                var types = member.Parameters.Select(p => p.Type).Except(methodSymbol.Parameters.Select(p => p.Type));
                return !types.Except(additionalParameterTypes).Any();
            }
        }
    }
}
