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
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, additionalParameterTypes) is not null;
    }

    public bool HasOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        IOperation currentOperation,
        params ITypeSymbol[] additionalParameterTypes)
    {
        if (currentOperation.SemanticModel is null)
            return false;

        return FindOverloadWithAdditionalParameterOfType(methodSymbol, syntaxNode: currentOperation.Syntax, includeObsoleteMethods: false, allowOptionalParameters: false, additionalParameterTypes) is not null;
    }

    private IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        params ITypeSymbol[] additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, includeObsoleteMethods: false, allowOptionalParameters: false, additionalParameterTypes);
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        bool includeObsoleteMethods,
        bool allowOptionalParameters,
        params ITypeSymbol[] additionalParameterTypes)
    {
        return FindOverloadWithAdditionalParameterOfType(methodSymbol, syntaxNode: null, includeObsoleteMethods, allowOptionalParameters, additionalParameterTypes);
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        IOperation operation,
        bool includeObsoleteMethods,
        bool allowOptionalParameters,
        params ITypeSymbol[] additionalParameterTypes)
    {
        if (operation.SemanticModel is null)
            return null;

        return FindOverloadWithAdditionalParameterOfType(methodSymbol, operation.Syntax, includeObsoleteMethods, allowOptionalParameters, additionalParameterTypes);
    }

    public IMethodSymbol? FindOverloadWithAdditionalParameterOfType(
        IMethodSymbol methodSymbol,
        SyntaxNode? syntaxNode,
        bool includeObsoleteMethods,
        bool allowOptionalParameters,
        params ITypeSymbol[] additionalParameterTypes)
    {
        if (additionalParameterTypes is null)
            return null;

        additionalParameterTypes = additionalParameterTypes.Where(type => type is not null).ToArray();
        if (additionalParameterTypes.Length == 0)
            return null;

        ImmutableArray<ISymbol> members;
        if (syntaxNode is not null)
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

                if (HasSimilarParameters(methodSymbol, method, allowOptionalParameters, additionalParameterTypes))
                    return method;
            }
        }

        return null;
    }

    /// <summary>
    /// Methods are similar if:
    /// <list type="bullet">
    /// <item><paramref name="method"/> and <paramref name="otherMethod"/> are not the same method</item>
    /// <item><paramref name="method"/> and <paramref name="otherMethod"/> have parameters of the same types, order is not important</item>
    /// <item><paramref name="otherMethod"/> can have additional parameters of type specified by <paramref name="additionalParameterTypes"/></item>
    /// <item>If <paramref name="allowOptionalParameters"/>, <paramref name="otherMethod"/> can have more parameters if they are optional</item>
    /// </list>
    /// </summary>
    public static bool HasSimilarParameters(IMethodSymbol method, IMethodSymbol otherMethod, bool allowOptionalParameters, params ITypeSymbol[] additionalParameterTypes)
    {
        if (method.IsEqualTo(otherMethod))
            return false;

        // The new method must have at least the same number of parameters as the old method, plus the number of additional parameters        
        if (otherMethod.Parameters.Length - method.Parameters.Length < additionalParameterTypes.Length)
            return false;

        // If allowOptionalParameters is false, the new method must have exactly the same number of parameters as the old method
        if (!allowOptionalParameters && otherMethod.Parameters.Length - method.Parameters.Length != additionalParameterTypes.Length)
            return false;

        // Most of the time, an overload has the same order for the parameters. Try to match them in order first (faster)
        {
            int i = 0, j = 0;
            var additionalParameterIndex = 0;
            while (i < method.Parameters.Length && j < otherMethod.Parameters.Length)
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

            if (i == method.Parameters.Length && j == otherMethod.Parameters.Length && additionalParameterIndex == additionalParameterTypes.Length)
                return true;
        }

        // Slower search, allows to find overload with different parameter order
        // Also, handle allow optional parameters
        {
            var otherMethodParameters = otherMethod.Parameters;

            foreach (var param in method.Parameters)
            {
                var found = false;
                for (var i = 0; i < otherMethodParameters.Length; i++)
                {
                    if (otherMethodParameters[i].Type.IsEqualTo(param.Type))
                    {
                        otherMethodParameters = otherMethodParameters.RemoveAt(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            foreach (var paramType in additionalParameterTypes)
            {
                var found = false;
                for (var i = 0; i < otherMethodParameters.Length; i++)
                {
                    if (otherMethodParameters[i].Type.IsEqualTo(paramType))
                    {
                        otherMethodParameters = otherMethodParameters.RemoveAt(i);
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            if (otherMethodParameters.Length == 0)
                return true;

            if (allowOptionalParameters)
            {
                if (otherMethodParameters.All(p => p.IsOptional))
                    return true;
            }

            return false;
        }
    }

    private bool IsObsolete(IMethodSymbol methodSymbol)
    {
        if (_obsoleteSymbol is null)
            return false;

        return methodSymbol.HasAttribute(_obsoleteSymbol);
    }
}
