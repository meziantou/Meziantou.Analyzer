using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;
internal sealed class OverloadFinder(Compilation compilation)
{
    private readonly ITypeSymbol? _obsoleteSymbol = compilation.GetBestTypeByMetadataName("System.ObsoleteAttribute");

    public bool HasOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        params ITypeSymbol[] additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, additionalParameterTypes) != null;
    }

    public bool HasOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        IOperation currentOperation,
        params ITypeSymbol[] additionalParameterTypes)
    {
        if (currentOperation.SemanticModel == null)
            return false;

        return FindOverloadWithAdditionalParameterOfType(methodSymbol, syntaxNode: currentOperation.Syntax, includeObsoleteMethods: false, additionalParameterTypes) != null;
    }

    private IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        params ITypeSymbol[] additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, includeObsoleteMethods: false, additionalParameterTypes);
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        bool includeObsoleteMethods,
        params ITypeSymbol[] additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, syntaxNode: null, includeObsoleteMethods, additionalParameterTypes);
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        IOperation operation,
        bool includeObsoleteMethods,
        params ITypeSymbol[] additionalParameterTypes)
    {
        if (operation.SemanticModel == null)
            return null;

        return FindOverloadWithAdditionalParameterOfType(methodSymbol, operation.Syntax, includeObsoleteMethods, additionalParameterTypes);
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        SyntaxNode? syntaxNode,
        bool includeObsoleteMethods,
        params ITypeSymbol[] additionalParameterTypes)
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

        foreach (var member in members)
        {
            if (member is IMethodSymbol method)
            {
                if (!includeObsoleteMethods && IsObsolete(method))
                    continue;

                if (HasSimilarParameters(methodSymbol, method, additionalParameterTypes))
                    return method;
            }
        }

        return null;
    }

    public static bool HasSimilarParameters(IMethodSymbol method, IMethodSymbol otherMethod, params ITypeSymbol[] additionalParameterTypes)
    {
        if (method.IsEqualTo(otherMethod))
            return false;

        if (otherMethod.Parameters.Length - method.Parameters.Length != additionalParameterTypes.Length)
            return false;

        // Most of the time, an overload has the same order for the parameters
        {
            int i = 0, j = 0;
            var additionalParameterIndex = 0;
            while (i < method.Parameters.Length && j < method.Parameters.Length)
            {
                var methodParameter = method.Parameters[i];
                var otherMethodParameter = otherMethod.Parameters[j];

                if (methodParameter.IsEqualTo(otherMethodParameter))
                {
                    i++;
                    j++;
                    continue;
                }

                if (additionalParameterIndex == additionalParameterTypes.Length)
                    break;

                var additionalParameter = additionalParameterTypes[additionalParameterIndex];
                if (methodParameter.Type.IsEqualTo(additionalParameter))
                {
                    i++;
                    continue;
                }

                if (otherMethodParameter.Type.IsEqualTo(additionalParameter))
                {
                    j++;
                    continue;
                }

                break;
            }

            if (i == method.Parameters.Length && j == otherMethod.Parameters.Length)
                return true;
        }

        // Slower search, allows to find overload with different parameter order
        {
            var otherMethodParameters = otherMethod.Parameters;

            foreach (var param in method.Parameters)
            {
                for (var i = 0; i < otherMethodParameters.Length; i++)
                {
                    if (otherMethodParameters[i].Type.IsEqualTo(param.Type))
                    {
                        otherMethodParameters = otherMethodParameters.RemoveAt(i);
                        break;
                    }
                }
            }

            foreach (var paramType in additionalParameterTypes)
            {
                for (var i = 0; i < otherMethodParameters.Length; i++)
                {
                    if (otherMethodParameters[i].Type.IsEqualTo(paramType))
                    {
                        otherMethodParameters = otherMethodParameters.RemoveAt(i);
                        break;
                    }
                }
            }

            return otherMethodParameters.Length == 0;
        }
    }

    private bool IsObsolete(IMethodSymbol methodSymbol)
    {
        if (_obsoleteSymbol == null)
            return false;

        return methodSymbol.HasAttribute(_obsoleteSymbol);
    }
}
