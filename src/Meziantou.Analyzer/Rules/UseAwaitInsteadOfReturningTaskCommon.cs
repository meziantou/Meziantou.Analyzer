using System.Runtime.CompilerServices;

namespace Meziantou.Analyzer.Rules;

internal static class UseAwaitInsteadOfReturningTaskCommon
{
    /// <summary>
    /// Determines whether the <see langword="async"/> modifier can be added to the function without breaking the compilation.
    /// </summary>
    internal static bool CanBeMadeAsync(IMethodSymbol method, Compilation compilation)
    {
        // Only members that support the 'async' keyword can be reported. Property/event accessors, operators,
        // constructors, etc. cannot be async even though they may return an awaitable type.
        if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.LocalFunction or MethodKind.LambdaMethod or MethodKind.AnonymousFunction))
            return false;

        // 'async' cannot be combined with a by-reference return type (CS1073)
        if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            return false;

        // An instance of a ref struct cannot be preserved across an 'await' boundary, so the instance methods
        // of a ref struct cannot be async (CS4007). Local functions and lambdas already cannot capture the
        // 'this' of a ref struct, so only the members themselves are excluded.
        if (method.MethodKind is (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation) && !method.IsStatic && method.ContainingType is { IsRefLikeType: true })
            return false;

        // 'MethodImplOptions.Synchronized' cannot be applied to an async method (CS4015)
        if (IsSynchronized(method, compilation))
            return false;

        foreach (var parameter in method.Parameters)
        {
            // Async methods cannot have ref, in or out parameters (CS1988)
            if (parameter.RefKind is not RefKind.None)
                return false;

            // Async methods cannot have pointer type parameters (CS4005)
            if (parameter.Type is IPointerTypeSymbol or IFunctionPointerTypeSymbol)
                return false;

            // Parameters of a ref struct type cannot be declared in async methods (CS4012)
            if (IsRefLikeOrAllowsRefLike(parameter.Type))
                return false;
        }

        return true;
    }

    private static bool IsRefLikeOrAllowsRefLike(ITypeSymbol type)
    {
        if (type.IsRefLikeType)
            return true;

#if ROSLYN_4_14_OR_GREATER
        if (type is ITypeParameterSymbol { AllowsRefLikeType: true })
            return true;
#endif

        return false;
    }

    private static bool IsSynchronized(IMethodSymbol method, Compilation compilation)
    {
        var methodImplAttributeSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.MethodImplAttribute");
        if (methodImplAttributeSymbol is null)
            return false;

        foreach (var attribute in method.GetAttributes())
        {
            if (!attribute.AttributeClass.IsEqualTo(methodImplAttributeSymbol))
                continue;

            foreach (var argument in attribute.ConstructorArguments)
            {
                if (HasSynchronizedFlag(argument))
                    return true;
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (HasSynchronizedFlag(argument.Value))
                    return true;
            }
        }

        return false;

        static bool HasSynchronizedFlag(TypedConstant constant)
        {
            var value = constant.Value switch
            {
                short shortValue => shortValue,
                int intValue => intValue,
                _ => 0,
            };

            return (value & (int)MethodImplOptions.Synchronized) is not 0;
        }
    }
}
