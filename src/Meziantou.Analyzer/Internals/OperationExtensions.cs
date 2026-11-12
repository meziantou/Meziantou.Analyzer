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
        var semanticModel = operation.SemanticModel;

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

    /// <summary>Finds the symbols named <paramref name="name"/> that can be used at the operation.</summary>
    public static IEnumerable<ISymbol> LookupAvailableSymbols(this IOperation operation, string name, CancellationToken cancellationToken)
    {
        var semanticModel = operation.SemanticModel!;
        var operationLocation = operation.Syntax.SpanStart;
        var enclosingType = GetEnclosingType(semanticModel.GetEnclosingSymbol(operationLocation, cancellationToken));
        return FilterAvailableSymbols(operation, semanticModel.LookupSymbols(operationLocation, name: name), operationLocation, enclosingType, cancellationToken);
    }

    /// <summary>
    /// Finds the locals, the parameters, the fields and the properties that can be used at the operation, among the ones for which
    /// <paramref name="isCandidate"/> returns <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Each call to <see cref="SemanticModel.LookupSymbols"/> walks all the scopes of the position, including the using directives,
    /// and looking up all the names binds every name in scope, which is expensive. Instead, the values declared in scope are listed
    /// from the syntax of the enclosing member and from the symbols of the enclosing functions and types, and only the names of the
    /// candidates are looked up. A name that is looked up is resolved the same way, such as a local hiding a field, so the values are
    /// the same as the ones of a lookup of all the names.
    /// </remarks>
    public static IEnumerable<ISymbol> LookupAvailableValues(this IOperation operation, Func<ISymbol, bool> isCandidate, CancellationToken cancellationToken)
    {
        var semanticModel = operation.SemanticModel!;
        var operationLocation = operation.Syntax.SpanStart;
        var enclosingSymbol = semanticModel.GetEnclosingSymbol(operationLocation, cancellationToken);
        var enclosingType = GetEnclosingType(enclosingSymbol);

        // A script can use the members of its host object and the values declared by the previous submissions
        if (semanticModel.Compilation.ScriptCompilationInfo is not null)
            return FilterAvailableSymbols(operation, semanticModel.LookupSymbols(operationLocation).Where(symbol => IsValue(symbol) && isCandidate(symbol)), operationLocation, enclosingType, cancellationToken);

        var names = GetCandidateValueNames(operation, semanticModel, operationLocation, enclosingSymbol, enclosingType, isCandidate, cancellationToken);
        if (names.Count == 0)
            return [];

        var symbols = names.SelectMany(name => semanticModel.LookupSymbols(operationLocation, name: name)).Where(IsValue);
        return FilterAvailableSymbols(operation, symbols, operationLocation, enclosingType, cancellationToken);

        static bool IsValue(ISymbol symbol) => symbol is ILocalSymbol or IParameterSymbol or IFieldSymbol or IPropertySymbol && symbol.CanBeReferencedByName;
    }

    private static INamedTypeSymbol? GetEnclosingType(ISymbol? enclosingSymbol) => enclosingSymbol as INamedTypeSymbol ?? enclosingSymbol?.ContainingType;

    /// <summary>
    /// Gets the names of the candidate values declared in scope at <paramref name="position"/>: the locals of the enclosing member, the
    /// parameters of the enclosing functions, of the primary constructors and of the extension blocks, the fields and the properties of
    /// the enclosing types, and the ones of the types imported by the <c>using static</c> directives. A value can be declared in scope
    /// and not be usable at the position, such as a local declared in another block, so the names must be looked up.
    /// </summary>
    private static HashSet<string> GetCandidateValueNames(IOperation operation, SemanticModel semanticModel, int position, ISymbol? enclosingSymbol, INamedTypeSymbol? enclosingType, Func<ISymbol, bool> isCandidate, CancellationToken cancellationToken)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        // The locals are declared in the enclosing member. The locals of the top-level statements are declared in any global statement of the file.
        foreach (var node in operation.Syntax.AncestorsAndSelf())
        {
            if (node is GlobalStatementSyntax { Parent: CompilationUnitSyntax compilationUnit })
            {
                foreach (var member in compilationUnit.Members)
                {
                    if (member is GlobalStatementSyntax)
                    {
                        AddLocals(member);
                    }
                }

                break;
            }

            if (node.Parent is BaseTypeDeclarationSyntax or BaseNamespaceDeclarationSyntax or CompilationUnitSyntax)
            {
                AddLocals(node);
                break;
            }
        }

        // The parameters of the enclosing lambdas, local functions and method, including the implicit ones such as "value" in a setter
        for (var symbol = enclosingSymbol; symbol is IMethodSymbol method; symbol = method.ContainingSymbol)
        {
            AddRange(method.Parameters);
        }

        for (var type = enclosingType; type is not null; type = type.ContainingType)
        {
            AddValueMembers(type);

            // The parameters of a primary constructor, which can be declared in another part of a partial type
            foreach (var constructor in type.InstanceConstructors)
            {
                AddRange(constructor.Parameters);
            }

            if (OverloadCandidateIndex.IsExtensionBlock(type) && type.ExtensionParameter is { } extensionParameter)
            {
                Add(extensionParameter);
            }

            // The default implementations of an interface can use the members of the interfaces it inherits from
            if (type.TypeKind is TypeKind.Interface)
            {
                foreach (var @interface in type.AllInterfaces)
                {
                    AddValueMembers(@interface);
                }
            }
        }

        foreach (var importScope in semanticModel.GetImportScopes(position, cancellationToken))
        {
            foreach (var import in importScope.Imports)
            {
                if (import.NamespaceOrType is INamedTypeSymbol importedType)
                {
                    AddValueMembers(importedType);
                }
            }
        }

        return names;

        void AddLocals(SyntaxNode root)
        {
            foreach (var node in root.DescendantNodes())
            {
                // A local declared after the operation cannot be used
                if (node.SpanStart > position)
                    break;

                if (node is VariableDeclaratorSyntax or SingleVariableDesignationSyntax or ForEachStatementSyntax or CatchDeclarationSyntax && semanticModel.GetDeclaredSymbol(node, cancellationToken) is { } symbol)
                {
                    Add(symbol);
                }
            }
        }

        void AddValueMembers(INamedTypeSymbol type)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IFieldSymbol or IPropertySymbol)
                    {
                        Add(member);
                    }
                }
            }
        }

        void AddRange(ImmutableArray<IParameterSymbol> parameters)
        {
            foreach (var parameter in parameters)
            {
                Add(parameter);
            }
        }

        void Add(ISymbol symbol)
        {
            if (!names.Contains(symbol.Name) && isCandidate(symbol))
            {
                names.Add(symbol.Name);
            }
        }
    }

    private static IEnumerable<ISymbol> FilterAvailableSymbols(IOperation operation, IEnumerable<ISymbol> symbols, int operationLocation, INamedTypeSymbol? enclosingType, CancellationToken cancellationToken)
    {
        var staticContext = GetStaticContext(operation, cancellationToken);
        foreach (var symbol in symbols)
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
