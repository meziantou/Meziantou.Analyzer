using System.Collections.Concurrent;

namespace Meziantou.Analyzer.Internals;
internal sealed class AwaitableTypes
{
    private const string GetAwaiterMethodName = "GetAwaiter";
    private const int MaxConstraintDepth = 8;

    private readonly INamedTypeSymbol[] _taskOrValueTaskSymbols;
    private readonly HashSet<INamedTypeSymbol> _nonAwaitableTypes;
    private readonly Compilation _compilation;
    private readonly ConcurrentDictionary<ITypeSymbol, bool> _isAwaitableCache = new(SymbolEqualityComparer.Default);

    // Most of the types have no GetAwaiter method, and the answer does not depend on where the type is used
    private readonly ConcurrentDictionary<ITypeSymbol, bool> _mightHaveGetAwaiterMethodCache = new(SymbolEqualityComparer.Default);
    private readonly Func<ITypeSymbol, bool> _mightHaveGetAwaiterMethod;

    public AwaitableTypes(Compilation compilation)
    {
        INotifyCompletionSymbol = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.INotifyCompletion");
        AsyncMethodBuilderAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
        IAsyncEnumerableSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        IAsyncEnumeratorSymbol = compilation.GetTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");
        TaskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        ValueTaskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        ValueTaskOfTSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (INotifyCompletionSymbol is not null)
        {
            var taskLikeSymbols = new List<INamedTypeSymbol>(4);
            taskLikeSymbols.AddIfNotNull(TaskSymbol);
            taskLikeSymbols.AddIfNotNull(TaskOfTSymbol);
            taskLikeSymbols.AddIfNotNull(ValueTaskSymbol);
            taskLikeSymbols.AddIfNotNull(ValueTaskOfTSymbol);
            _taskOrValueTaskSymbols = [.. taskLikeSymbols];
        }
        else
        {
            _taskOrValueTaskSymbols = [];
        }

        _nonAwaitableTypes = CreateNonAwaitableTypes(compilation);
        _compilation = compilation;
        _mightHaveGetAwaiterMethod = MightHaveGetAwaiterMethod;
    }

    public INamedTypeSymbol? TaskSymbol { get; }
    private INamedTypeSymbol? TaskOfTSymbol { get; }
    private INamedTypeSymbol? ValueTaskSymbol { get; }
    private INamedTypeSymbol? ValueTaskOfTSymbol { get; }
    private INamedTypeSymbol? INotifyCompletionSymbol { get; }
    private INamedTypeSymbol? AsyncMethodBuilderAttributeSymbol { get; }
    public INamedTypeSymbol? IAsyncEnumerableSymbol { get; }
    public INamedTypeSymbol? IAsyncEnumeratorSymbol { get; }

    public bool IsNonAwaitableType(ITypeSymbol? symbol)
    {
        if (_nonAwaitableTypes.Count == 0 || symbol is not INamedTypeSymbol namedType)
            return false;

        return IsNonAwaitableTypeCore(namedType);
    }

    private bool IsNonAwaitableTypeCore(INamedTypeSymbol type)
    {
        if (_nonAwaitableTypes.Contains(type))
            return true;

        if (!ReferenceEquals(type, type.OriginalDefinition) && _nonAwaitableTypes.Contains(type.OriginalDefinition))
            return true;

        return false;
    }

    private static HashSet<INamedTypeSymbol> CreateNonAwaitableTypes(Compilation compilation)
    {
        var result = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var attribute in compilation.Assembly.GetAttributes())
        {
            if (!AnnotationAttributes.IsNonAwaitableTypeAttributeSymbol(attribute.AttributeClass))
                continue;

            var constructorArguments = attribute.ConstructorArguments;
            if (constructorArguments is [{ Value: INamedTypeSymbol type }])
            {
                result.Add(type);
                if (!ReferenceEquals(type.OriginalDefinition, type))
                {
                    result.Add(type.OriginalDefinition);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Indicates whether <see cref="IsAwaitable(ITypeSymbol?, SemanticModel, int)"/> can return <see langword="true"/> for the
    /// type, wherever the type is used. This is cheap to check, and it rejects most of the types, so it can be used to skip
    /// the other checks of a rule before determining whether the type is awaitable at a given position.
    /// </summary>
    public bool MightBeAwaitable([NotNullWhen(true)] ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (INotifyCompletionSymbol is null)
            return false;

        if (IsNonAwaitableType(symbol))
            return false;

        if (symbol.SpecialType is SpecialType.System_Void || symbol.TypeKind is TypeKind.Dynamic)
            return false;

        if (IsTaskOrValueTask(symbol))
            return true;

        return _mightHaveGetAwaiterMethodCache.GetOrAdd(symbol, _mightHaveGetAwaiterMethod);
    }

    // https://github.com/dotnet/roslyn/blob/248e85149427c534c4a156a436ecff69bab83b59/src/Compilers/CSharp/Portable/Binder/Binder_Await.cs#L347
    public bool IsAwaitable(ITypeSymbol? symbol, SemanticModel semanticModel, int position)
    {
        // Looking up the symbols in scope with the semantic model is expensive, so it is only done for the types that can have a GetAwaiter method
        if (!MightBeAwaitable(symbol))
            return false;

        if (IsTaskOrValueTask(symbol))
            return true;

        foreach (var potentialSymbol in semanticModel.LookupSymbols(position, container: symbol, name: GetAwaiterMethodName, includeReducedExtensionMethods: true))
        {
            if (potentialSymbol is not IMethodSymbol getAwaiterMethod)
                continue;

            if (!semanticModel.IsAccessible(position, getAwaiterMethod))
                continue;

            if (!getAwaiterMethod.Parameters.IsEmpty)
                continue;

            if (!ConformsToAwaiterPattern(getAwaiterMethod.ReturnType))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Indicates whether a lookup of the <c>GetAwaiter</c> methods of the type can find a method without parameters, wherever
    /// the lookup is done: a method of the type, of its base types or of its interfaces, or an extension method of the
    /// compilation or of its references whose receiver can be an instance of the type. The imported namespaces and the
    /// accessibility of the methods are ignored, so the lookup is still needed when this method returns <see langword="true"/>.
    /// </summary>
    private bool MightHaveGetAwaiterMethod(ITypeSymbol symbol)
    {
        var lookupTypes = new List<ITypeSymbol>();
        if (!AddLookupTypes(lookupTypes, symbol, depth: 0))
            return true;

        foreach (var lookupType in lookupTypes)
        {
            foreach (var member in lookupType.GetMembers(GetAwaiterMethodName))
            {
                if (member is IMethodSymbol { Parameters.IsEmpty: true })
                    return true;
            }
        }

        if (!OverloadCandidateIndex.Get(_compilation).TryGetExtensionMethods(GetAwaiterMethodName, out var extensionMethods))
            return true;

        foreach (var extensionMethod in extensionMethods)
        {
            if (MightBeExtensionMethodOf(extensionMethod, lookupTypes))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Adds the types whose members a member lookup in the type can find, which are also the types of the receivers of the
    /// extension methods that can be used on an instance of the type, except for the receivers handled by
    /// <see cref="MightBeExtensionMethodOf"/>. Returns <see langword="false"/> when they are not known.
    /// </summary>
    private bool AddLookupTypes(List<ITypeSymbol> lookupTypes, ITypeSymbol type, int depth)
    {
        switch (type)
        {
            case INamedTypeSymbol or IArrayTypeSymbol when type.TypeKind is not TypeKind.Error:
                for (var current = type; current is not null; current = current.BaseType)
                {
                    lookupTypes.Add(current);
                }

                lookupTypes.AddRange(type.AllInterfaces);

                // The members of System.Object are available on the interfaces
                if (type.TypeKind is TypeKind.Interface)
                {
                    lookupTypes.Add(_compilation.ObjectType);
                }

                // A nullable value type can be boxed to the interfaces of its underlying type
                if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [var underlyingType] } && depth < MaxConstraintDepth)
                    return AddLookupTypes(lookupTypes, underlyingType, depth + 1);

                return true;

            // The members of a type parameter are the ones of its constraints
            case ITypeParameterSymbol typeParameter when depth < MaxConstraintDepth:
                foreach (var constraintType in typeParameter.ConstraintTypes)
                {
                    if (!AddLookupTypes(lookupTypes, constraintType, depth + 1))
                        return false;
                }

                lookupTypes.Add(_compilation.ObjectType);
                lookupTypes.Add(_compilation.GetSpecialType(SpecialType.System_ValueType));
                return true;

            default:
                return false;
        }
    }

    private static bool MightBeExtensionMethodOf(IMethodSymbol extensionMethod, List<ITypeSymbol> lookupTypes)
    {
        // The receiver of the members of an extension block is not a parameter of the method
        if (OverloadCandidateIndex.IsExtensionBlock(extensionMethod.ContainingType))
            return true;

        // The lookup reduces the extension methods, so the receiver is the only parameter of a method without parameters
        if (extensionMethod.Parameters is not [{ Type: var receiverType }])
            return false;

        // A type parameter can be any type, an array can be converted to an array of another type, and a span can be
        // converted from an array, a string or another span
        if (receiverType is not INamedTypeSymbol { IsRefLikeType: false })
            return true;

        // The type arguments can differ, as they can be the type parameters of the method, or be variant
        var receiverTypeDefinition = receiverType.OriginalDefinition;
        foreach (var lookupType in lookupTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(lookupType.OriginalDefinition, receiverTypeDefinition))
                return true;
        }

        return false;
    }

    public bool IsAwaitable(ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        return _isAwaitableCache.GetOrAdd(symbol, IsAwaitableCore);
    }

    private bool IsAwaitableCore(ITypeSymbol symbol)
    {
        if (INotifyCompletionSymbol is null)
            return false;

        if (IsNonAwaitableType(symbol))
            return false;

        if (symbol.SpecialType is SpecialType.System_Void || symbol.TypeKind is TypeKind.Dynamic)
            return false;

        if (IsTaskOrValueTask(symbol))
            return true;

        var awaiter = symbol.GetMembers(GetAwaiterMethodName);

        foreach (var potentialSymbol in awaiter)
        {
            if (potentialSymbol is not IMethodSymbol getAwaiterMethod)
                continue;

            if (!_compilation.IsSymbolAccessibleWithin(potentialSymbol, _compilation.Assembly))
                continue;

            if (!getAwaiterMethod.Parameters.IsEmpty)
                continue;

            if (!ConformsToAwaiterPattern(getAwaiterMethod.ReturnType))
                continue;

            return true;
        }

        return false;
    }

    public bool DoesNotReturnVoidAndCanUseAsyncKeyword(IMethodSymbol method, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        if (method.IsTopLevelStatementsEntryPointMethod())
            return true;

        if (method.ReturnsVoid)
        {
            // Task.Run(()=>{}) => Task.Run(async ()=>{})
            if (method.DeclaringSyntaxReferences.Length == 1)
            {
                var syntax = method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
                var methodOperation = semanticModel.GetOperation(syntax, cancellationToken);
                if (methodOperation is { Parent: IDelegateCreationOperation { Parent: IArgumentOperation { Parent: IInvocationOperation invocation } } })
                {
                    if (invocation.TargetMethod.Name is "Run" && invocation.TargetMethod.ContainingType.IsEqualTo(TaskSymbol))
                        return true;
                }
            }

            return false;
        }

        if (method.IsAsync)
            return true;

        if (IsTaskOrValueTask(method.ReturnType))
            return true;

        if (method.ReturnType is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.ConstructedFrom.IsEqualToAny(IAsyncEnumerableSymbol, IAsyncEnumeratorSymbol))
            return true;

        if (AsyncMethodBuilderAttributeSymbol is not null && method.ReturnType.HasAttribute(AsyncMethodBuilderAttributeSymbol))
            return true;

        return false;
    }

    /// <summary>
    /// Determines whether the operation always produces an already-completed task, such as
    /// <c>Task.CompletedTask</c>, <c>Task.FromResult(value)</c> or <c>new ValueTask&lt;T&gt;(value)</c>.
    /// Awaiting such a task does not introduce any asynchrony.
    /// </summary>
    public bool IsCompletedTask(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        // Task.CompletedTask, ValueTask.CompletedTask
        if (operation is IPropertyReferenceOperation propertyReference)
            return propertyReference.Property.Name is nameof(Task.CompletedTask) && propertyReference.Property.ContainingType.IsEqualToAny(TaskSymbol, ValueTaskSymbol);

        // Task.FromResult, Task.FromCanceled, Task.FromException and their ValueTask counterparts
        if (operation is IInvocationOperation invocation)
            return invocation.TargetMethod.Name is nameof(Task.FromResult) or nameof(Task.FromCanceled) or nameof(Task.FromException) && invocation.TargetMethod.ContainingType.IsEqualToAny(TaskSymbol, ValueTaskSymbol);

        if (operation is IObjectCreationOperation objectCreation && objectCreation.Type is INamedTypeSymbol type)
        {
            // new ValueTask()
            if (type.OriginalDefinition.IsEqualTo(ValueTaskSymbol) && objectCreation.Arguments.Length == 0)
                return true;

            // new ValueTask<T>(T value)
            if (type.OriginalDefinition.IsEqualTo(ValueTaskOfTSymbol) && objectCreation.Arguments is [{ Parameter: { } parameter }] && parameter.Type.IsEqualTo(type.TypeArguments[0]))
                return true;
        }

        return false;
    }

    public bool IsAsyncBuildableAndNotVoid(ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (symbol.OriginalDefinition.IsEqualToAny(TaskSymbol, TaskOfTSymbol))
            return true;

        if (symbol.HasAttribute(AsyncMethodBuilderAttributeSymbol))
            return true;

        return false;
    }

    private bool IsTaskOrValueTask(ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        var originalDefinition = symbol.OriginalDefinition;
        foreach (var taskLikeSymbol in _taskOrValueTaskSymbols)
        {
            if (originalDefinition.IsEqualTo(taskLikeSymbol))
                return true;
        }

        return false;
    }

    private bool ConformsToAwaiterPattern(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is null)
            return false;

        var hasGetResultMethod = false;
        var hasIsCompletedProperty = false;

        if (!typeSymbol.Implements(INotifyCompletionSymbol))
            return false;

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IMethodSymbol { Name: "GetResult", Parameters.IsEmpty: true, TypeParameters.IsEmpty: true, IsStatic: false })
            {
                hasGetResultMethod = true;
            }
            else if (member is IPropertySymbol { Name: "IsCompleted", IsStatic: false, Type.SpecialType: SpecialType.System_Boolean, GetMethod: not null })
            {
                hasIsCompletedProperty = true;
            }
            else
            {
                continue;
            }

            if (hasGetResultMethod && hasIsCompletedProperty)
            {
                return true;
            }
        }

        return false;
    }
}
