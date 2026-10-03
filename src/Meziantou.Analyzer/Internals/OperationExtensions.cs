using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Internals;

internal static class OperationExtensions
{
    public static IOperation.OperationList GetChildOperations(this IOperation operation)
    {
        return operation.ChildOperations;
    }

    public static ITypeSymbol? GetActualType(this IOperation operation)
    {
        if (operation is IConversionOperation conversionOperation)
        {
            return GetActualType(conversionOperation.Operand);
        }

        return operation.Type;
    }

    public static bool HasArgumentOfType(this IInvocationOperation operation, ITypeSymbol? argumentTypeSymbol, bool inherits = false)
    {
        if (argumentTypeSymbol is null)
            return false;

        if (inherits && argumentTypeSymbol.IsSealed)
        {
            inherits = false;
        }

        foreach (var arg in operation.Arguments)
        {
            if (inherits)
            {
                if (arg.Value.Type is not null && arg.Value.Type.IsAssignableTo(argumentTypeSymbol))
                    return true;
            }
            else if (argumentTypeSymbol.IsEqualTo(arg.Value.Type))
                return true;
        }

        return false;
    }

    /// <summary>Indicates whether the parameters of the primary constructor of the containing type can be used at the operation.</summary>
    public static bool CanUsePrimaryConstructorParameters(this IOperation operation, CancellationToken cancellationToken) => GetStaticContext(operation, cancellationToken).CanUsePrimaryConstructorParameters;

    private static StaticContext GetStaticContext(IOperation operation, CancellationToken cancellationToken)
    {
        var semanticModel = operation.SemanticModel!;

        // Local functions can be nested, and an instance local function can be declared
        // in a static local function. So, you need to continue to check ancestors when a
        // local function is not static.
        foreach (var node in operation.Syntax.Ancestors())
        {
            switch (node)
            {
                case LocalFunctionStatementSyntax localFunction when semanticModel.GetDeclaredSymbol(localFunction, cancellationToken) is { IsStatic: true }:
                    return new StaticContext(IsStatic: true, localFunction.SpanStart, CanUsePrimaryConstructorParameters: false);

                case AnonymousFunctionExpressionSyntax anonymousFunction when semanticModel.GetSymbolInfo(anonymousFunction, cancellationToken).Symbol is { IsStatic: true }:
                    return new StaticContext(IsStatic: true, anonymousFunction.SpanStart, CanUsePrimaryConstructorParameters: false);

                // The instance members and the primary constructor parameters cannot be used in the arguments of ": this(...)" and ": base(...)"
                case ConstructorInitializerSyntax { Parent: ConstructorDeclarationSyntax constructor }:
                    return new StaticContext(IsStatic: true, constructor.SpanStart, CanUsePrimaryConstructorParameters: false);

                // The instance members cannot be used in the arguments of the base type of a primary constructor, but its parameters can
                case PrimaryConstructorBaseTypeSyntax baseType:
                    return new StaticContext(IsStatic: true, baseType.SpanStart, CanUsePrimaryConstructorParameters: true);

                // The instance members cannot be used in the initializer of a field, but the primary constructor parameters can when the field is not static
                case BaseFieldDeclarationSyntax field:
                    return new StaticContext(IsStatic: true, field.SpanStart, CanUsePrimaryConstructorParameters: !IsStaticMember(field));

                case BasePropertyDeclarationSyntax property:
                    {
                        var isStatic = IsStaticMember(property);

                        // The instance members cannot be used in the initializer of a property, but the primary constructor parameters can when the property is not static
                        if (property is PropertyDeclarationSyntax { Initializer: { } initializer } && initializer.Span.Contains(operation.Syntax.Span))
                            return new StaticContext(IsStatic: true, property.SpanStart, CanUsePrimaryConstructorParameters: !isStatic);

                        return new StaticContext(isStatic, property.SpanStart, CanUsePrimaryConstructorParameters: !isStatic);
                    }

                // Methods, constructors, operators, conversion operators and finalizers
                case BaseMethodDeclarationSyntax method:
                    {
                        var isStatic = IsStaticMember(method);
                        return new StaticContext(isStatic, method.SpanStart, CanUsePrimaryConstructorParameters: !isStatic);
                    }

                // The operation is not in a member, such as in an attribute
                case BaseTypeDeclarationSyntax:
                    return StaticContext.Instance;
            }
        }

        return StaticContext.Instance;

        static bool IsStaticMember(MemberDeclarationSyntax member) => member.Modifiers.Any(SyntaxKind.StaticKeyword) || member.Modifiers.Any(SyntaxKind.ConstKeyword);
    }

    /// <param name="IsStatic">Indicates whether the instance members cannot be used.</param>
    /// <param name="StartPosition">The position of the static context. The locals and the parameters declared before it cannot be used, except the primary constructor parameters.</param>
    /// <param name="CanUsePrimaryConstructorParameters">Indicates whether the parameters of the primary constructor of the containing type can be used.</param>
    private readonly record struct StaticContext(bool IsStatic, int StartPosition, bool CanUsePrimaryConstructorParameters)
    {
        public static StaticContext Instance { get; } = new(IsStatic: false, StartPosition: -1, CanUsePrimaryConstructorParameters: true);
    }

    public static IEnumerable<ISymbol> LookupAvailableSymbols(this IOperation operation, CancellationToken cancellationToken)
    {
        // Find available symbols
        var semanticModel = operation.SemanticModel!;
        var operationLocation = operation.Syntax.GetLocation().SourceSpan.Start;
        var staticContext = GetStaticContext(operation, cancellationToken);
        var enclosingSymbol = semanticModel.GetEnclosingSymbol(operationLocation, cancellationToken);
        var enclosingType = enclosingSymbol as INamedTypeSymbol ?? enclosingSymbol?.ContainingType;
        foreach (var symbol in semanticModel.LookupSymbols(operationLocation))
        {
            // LookupSymbols check the accessibility of the symbol, but it can suggest instance members when the current
            // context is static, or when they are declared by a containing type of the current type.
            if (symbol is IFieldSymbol or IPropertySymbol && !symbol.IsStatic && (staticContext.IsStatic || !IsMemberOfType(symbol, enclosingType)))
                continue;

            if (symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } constructor } && constructor.IsPrimaryConstructor(cancellationToken, includeRecordDeclarations: true))
            {
                // The primary constructor parameters are declared outside the members, and can be declared in another part of a partial type
                if (!staticContext.CanUsePrimaryConstructorParameters || !constructor.ContainingType.OriginalDefinition.IsEqualTo(enclosingType?.OriginalDefinition))
                    continue;

                yield return symbol;
                continue;
            }

            // Locals can be returned even if there are not valid in the current context. For instance,
            // it can return locals declared after the current location. Or it can return locals that
            // should not be accessible in a static local function.
            //
            // void Sample()
            // {
            //    int local = 0;
            //    static void LocalFunction() => local; <-- local is invalid here but LookupSymbols suggests it
            // }
            //
            // Parameters from the ancestor methods are also returned even if the operation is in a static local function.
            if (symbol.Kind is SymbolKind.Local or SymbolKind.Parameter)
            {
                var isValid = true;
                foreach (var location in symbol.Locations)
                {
                    isValid &= IsValid(location, operationLocation, staticContext.IsStatic ? staticContext.StartPosition : null);
                    if (!isValid)
                        break;
                }

                if (!isValid)
                    continue;

                static bool IsValid(Location location, int operationLocation, int? staticContextStart)
                {
                    var localPosition = location.SourceSpan.Start;

                    // The local is declared after the current expression
                    if (localPosition > operationLocation)
                        return false;

                    // The local is declared outside the static local function
                    if (staticContextStart.HasValue && localPosition < staticContextStart.GetValueOrDefault())
                        return false;

                    return true;
                }
            }

            if (symbol.Kind is SymbolKind.Local)
            {
                // var a = Sample(a); // cannot use "a"
                var ancestors = operation.Ancestors();
                var isInInitializer = false;
                foreach (var ancestor in ancestors)
                {
                    if (ancestor is IVariableDeclaratorOperation declaratorOperation)
                    {
                        if (declaratorOperation.Symbol.IsEqualTo(symbol))
                        {
                            isInInitializer = true;
                            break;
                        }
                    }
                }

                if (isInInitializer)
                    continue;

                // Cannot use variables declared in top-level statements when not in this context
                if (symbol.ContainingSymbol?.IsTopLevelStatement(cancellationToken) is true && operation.GetContainingMethod(cancellationToken)?.IsTopLevelStatement(cancellationToken) is not true)
                {
                    continue;
                }
            }

            yield return symbol;
        }

        static bool IsMemberOfType(ISymbol member, INamedTypeSymbol? type)
        {
            if (type is null)
                return false;

            var memberType = member.ContainingType.OriginalDefinition;
            for (var current = type; current is not null; current = current.BaseType)
            {
                if (current.OriginalDefinition.IsEqualTo(memberType))
                    return true;
            }

            // The default implementations of an interface can use the members of the interfaces it inherits from
            if (memberType.TypeKind is TypeKind.Interface)
            {
                foreach (var @interface in type.AllInterfaces)
                {
                    if (@interface.OriginalDefinition.IsEqualTo(memberType))
                        return true;
                }
            }

            return false;
        }
    }

    public static bool IsConstantZero(this IOperation operation) => operation is { ConstantValue: { HasValue: true, Value: 0 or 0L or 0u or 0uL or 0f or 0d or 0m } };
    public static bool IsNull(this IOperation operation) => operation is { ConstantValue: { HasValue: true, Value: null } };
}
