using System.Diagnostics;
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
                    if (ns.IsNamespace(new[] { "Microsoft", "VisualStudio", "TestTools", "UnitTesting" }) ||
                        ns.IsNamespace(new[] { "NUnit", "Framework" }) ||
                        ns.IsNamespace(new[] { "Xunit" }))
                    {
                        return true;
                    }

                    type = type.BaseType;
                }
            }

            return false;
        }

        internal static bool HasOverloadWithAdditionalParameterOfType(
            this IMethodSymbol methodSymbol,
            Compilation compilation,
            params ITypeSymbol?[] additionalParameterTypes)
        {
            return FindOverloadWithAdditionalParameterOfType(methodSymbol, compilation, additionalParameterTypes) != null;
        }

        internal static IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
            this IMethodSymbol methodSymbol,
            Compilation compilation,
            params ITypeSymbol?[] additionalParameterTypes)
        {
            return FindOverloadWithAdditionalParameterOfType(methodSymbol, compilation, includeObsoleteMethods: false, additionalParameterTypes);
        }

        internal static IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
            this IMethodSymbol methodSymbol,
            Compilation compilation,
            bool includeObsoleteMethods,
            params ITypeSymbol?[] additionalParameterTypes)
        {
            if (additionalParameterTypes == null)
                return null;

            additionalParameterTypes = additionalParameterTypes.Where(type => type != null).ToArray();
            if (additionalParameterTypes.Length == 0)
                return null;

            var members = methodSymbol.ContainingType.GetMembers(methodSymbol.Name);
            return members.OfType<IMethodSymbol>()
                .FirstOrDefault(member => (includeObsoleteMethods || !member.IsObsolete(compilation)) && HasSimilarParameters(methodSymbol, member, additionalParameterTypes));
        }

        internal static bool IsObsolete(this IMethodSymbol methodSymbol, Compilation compilation)
        {
            var obsoleteAttribute = compilation?.GetTypeByMetadataName("System.ObsoleteAttribute");
            if (obsoleteAttribute == null)
                return false;

            return methodSymbol.HasAttribute(obsoleteAttribute);
        }

        internal static bool HasSimilarParameters(this IMethodSymbol methodSymbol, IMethodSymbol otherMethod, params ITypeSymbol?[] additionalParameterTypes)
        {
            if (methodSymbol.IsEqualTo(otherMethod))
                return false;

            if (additionalParameterTypes.Any(type => type == null))
            {
                additionalParameterTypes = additionalParameterTypes.WhereNotNull().ToArray();
            }

            if (otherMethod.Parameters.Length - methodSymbol.Parameters.Length != additionalParameterTypes.Length)
                return false;

            var methodParameters = methodSymbol.Parameters.Select(p => p.Type).ToList();
            var otherMethodParameters = otherMethod.Parameters.Select(p => p.Type).ToList();

            foreach (var param in methodParameters)
            {
                otherMethodParameters.Remove(param);
            }

            foreach (var param in additionalParameterTypes)
            {
                Debug.Assert(param != null);
                otherMethodParameters.Remove(param);
            }

            return otherMethodParameters.Count == 0;
        }
    }
}
