using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Internals;
internal sealed class AwaitableTypes
{
    private readonly INamedTypeSymbol[] _taskOrValueTaskSymbols;
    private readonly HashSet<INamedTypeSymbol> _nonAwaitableTypes;
    private readonly Compilation _compilation;
    private readonly ConcurrentDictionary<ITypeSymbol, bool> _isAwaitableCache = new(SymbolEqualityComparer.Default);

    public AwaitableTypes(Compilation compilation)
    {
        INotifyCompletionSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.INotifyCompletion");
        AsyncMethodBuilderAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
        IAsyncEnumerableSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        IAsyncEnumeratorSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");
        TaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask");
        ValueTaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1");

        if (INotifyCompletionSymbol is not null)
        {
            var taskLikeSymbols = new List<INamedTypeSymbol>(4);
            taskLikeSymbols.AddIfNotNull(TaskSymbol);
            taskLikeSymbols.AddIfNotNull(TaskOfTSymbol);
            taskLikeSymbols.AddIfNotNull(valueTaskSymbol);
            taskLikeSymbols.AddIfNotNull(ValueTaskOfTSymbol);
            _taskOrValueTaskSymbols = [.. taskLikeSymbols];
        }
        else
        {
            _taskOrValueTaskSymbols = [];
        }

        _nonAwaitableTypes = CreateNonAwaitableTypes(compilation);
        _compilation = compilation;
    }

    private INamedTypeSymbol? TaskSymbol { get; }
    private INamedTypeSymbol? TaskOfTSymbol { get; }
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

    // https://github.com/dotnet/roslyn/blob/248e85149427c534c4a156a436ecff69bab83b59/src/Compilers/CSharp/Portable/Binder/Binder_Await.cs#L347
    public bool IsAwaitable(ITypeSymbol? symbol, SemanticModel semanticModel, int position)
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

        foreach (var potentialSymbol in semanticModel.LookupSymbols(position, container: symbol, name: "GetAwaiter", includeReducedExtensionMethods: true))
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

        var awaiter = symbol.GetMembers("GetAwaiter");

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
