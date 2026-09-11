namespace Meziantou.Analyzer.Rules;

internal static class UseTypeofInsteadOfGetTypeOnSealedTypeCommon
{
    /// <summary>
    /// Gets the type <c>typeof</c> must be used with when the invocation is a call to <c>object.GetType()</c> whose
    /// result is known at compile time, or <see langword="null"/> when the invocation must not be replaced.
    /// </summary>
    public static INamedTypeSymbol? GetKnownType(IInvocationOperation operation)
    {
        // System.Type hides Object.GetType() with "public new Type GetType()", so the containing type of the invoked
        // method also excludes the calls reported by MA0130
        if (operation.TargetMethod is not { Name: "GetType", IsStatic: false, Parameters.IsEmpty: true, ContainingType.SpecialType: SpecialType.System_Object })
            return null;

        var instance = operation.Instance;

        // A value type instance is implicitly boxed to call 'object.GetType()'
        while (instance is IConversionOperation { IsImplicit: true } conversion)
        {
            instance = conversion.Operand;
        }

        // Replacing the invocation removes the evaluation of the instance, so its side effects must be preserved.
        // This also excludes 'instance?.GetType()', which evaluates to null instead of the type when the instance is null.
        if (instance is null || !IsSideEffectFree(instance))
            return null;

        if (instance.Type is not INamedTypeSymbol { IsSealed: true } type)
            return null;

        // 'nullable.GetType()' returns the type of the underlying value, or throws when there is no value
        if (type.OriginalDefinition.SpecialType is SpecialType.System_Nullable_T)
            return null;

        // 'typeof' does not support tuple element names, so use the underlying type of the named tuples
        if (type.IsTupleType && type.TupleUnderlyingType is { } tupleUnderlyingType)
        {
            type = tupleUnderlyingType;
        }

        if (!CanBeNamed(type))
            return null;

        return type;
    }

    /// <summary>
    /// Gets whether the type can be used with <c>typeof</c>, which the anonymous types and the types that contain
    /// one cannot, such as <c>Container&lt;anonymous type&gt;</c>.
    /// </summary>
    private static bool CanBeNamed(ITypeSymbol type)
    {
        if (type.IsAnonymousType || type.TypeKind is TypeKind.Error)
            return false;

        if (type is IArrayTypeSymbol array)
            return CanBeNamed(array.ElementType);

        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.ContainingType is not null && !CanBeNamed(namedType.ContainingType))
                return false;

            foreach (var typeArgument in namedType.TypeArguments)
            {
                if (!CanBeNamed(typeArgument))
                    return false;
            }
        }

        return true;
    }

    private static bool IsSideEffectFree(IOperation operation) => operation switch
    {
        ILocalReferenceOperation => true,
        IParameterReferenceOperation => true,
        ILiteralOperation => true,
        IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance } => true,
        IFieldReferenceOperation { Instance: var fieldInstance } => fieldInstance is null || IsSideEffectFree(fieldInstance),
        // The properties and the indexers are excluded as their getter can execute any code, such as modifying a
        // state or throwing an exception
        _ => false,
    };
}
