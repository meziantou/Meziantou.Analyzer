using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer;

internal static class MethodSymbolExtensions
{
    private static readonly string[] s_msTestNamespaceParts = new[] { "Microsoft", "VisualStudio", "TestTools", "UnitTesting" };
    private static readonly string[] s_nunitNamespaceParts = new[] { "NUnit", "Framework" };
    private static readonly string[] s_xunitNamespaceParts = new[] { "Xunit" };

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

    public static bool HasOverloadWithAdditionalParameterOfType(
        this IMethodSymbol methodSymbol,
        Compilation compilation,
        params ITypeSymbol?[] additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, compilation, additionalParameterTypes) != null;
    }

    public static bool HasOverloadWithAdditionalParameterOfType(
        this IMethodSymbol methodSymbol,
        IOperation currentOperation,
        params ITypeSymbol?[] additionalParameterTypes)
    {
        if (currentOperation.SemanticModel == null)
            return false;

        return FindOverloadWithAdditionalParameterOfType(methodSymbol, currentOperation.SemanticModel.Compilation, syntaxNode: currentOperation.Syntax, includeObsoleteMethods: false, additionalParameterTypes) != null;
    }

    private static IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        this IMethodSymbol methodSymbol,
        Compilation compilation,
        params ITypeSymbol?[] additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, compilation, includeObsoleteMethods: false, additionalParameterTypes);
    }

    public static IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        this IMethodSymbol methodSymbol,
        Compilation compilation,
        bool includeObsoleteMethods,
        params ITypeSymbol?[] additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, compilation, syntaxNode: null, includeObsoleteMethods, additionalParameterTypes);
    }

    public static IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        this IMethodSymbol methodSymbol,
        IOperation operation,
        bool includeObsoleteMethods,
        params ITypeSymbol?[] additionalParameterTypes)
    {
        if (operation.SemanticModel == null)
            return null;

        return FindOverloadWithAdditionalParameterOfType(methodSymbol, operation.SemanticModel.Compilation, operation.Syntax, includeObsoleteMethods, additionalParameterTypes);
    }

    public static IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        this IMethodSymbol methodSymbol,
        Compilation compilation,
        SyntaxNode? syntaxNode,
        bool includeObsoleteMethods,
        params ITypeSymbol?[] additionalParameterTypes)
    {
        if (additionalParameterTypes == null)
            return null;

        additionalParameterTypes = additionalParameterTypes.Where(type => type != null).ToArray();
        if (additionalParameterTypes.Length == 0)
            return null;

        ImmutableArray<ISymbol> members;
        if (syntaxNode != null)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxNode.SyntaxTree);
            members = semanticModel.LookupSymbols(syntaxNode.GetLocation().SourceSpan.End, methodSymbol.ContainingType, methodSymbol.Name, includeReducedExtensionMethods: true);
        }
        else
        {
            members = methodSymbol.ContainingType.GetMembers(methodSymbol.Name);
        }

        return members.OfType<IMethodSymbol>()
            .FirstOrDefault(member => (includeObsoleteMethods || !member.IsObsolete(compilation)) && HasSimilarParameters(methodSymbol, member, additionalParameterTypes));
    }

    public static bool IsObsolete(this IMethodSymbol methodSymbol, Compilation compilation)
    {
        var obsoleteAttribute = compilation?.GetBestTypeByMetadataName("System.ObsoleteAttribute");
        if (obsoleteAttribute == null)
            return false;

        return methodSymbol.HasAttribute(obsoleteAttribute);
    }

    public static bool HasSimilarParameters(this IMethodSymbol methodSymbol, IMethodSymbol otherMethod, params ITypeSymbol?[] additionalParameterTypes)
    {
        if (methodSymbol.IsEqualTo(otherMethod))
            return false;

        if (additionalParameterTypes.Any(type => type == null))
        {
            additionalParameterTypes = additionalParameterTypes.WhereNotNull().ToArray();
        }

        var methodParameters = methodSymbol.Parameters.Select(p => p.Type).ToList();
        var otherMethodParameters = otherMethod.Parameters.Select(p => p.Type).ToList();

        if (otherMethodParameters.Count - methodParameters.Count != additionalParameterTypes.Length)
            return false;

        foreach (var param in methodParameters)
        {
            otherMethodParameters.Remove(param);
        }

        foreach (var param in additionalParameterTypes)
        {
            otherMethodParameters.Remove(param!);
        }

        return otherMethodParameters.Count == 0;
    }
}
