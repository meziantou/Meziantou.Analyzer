using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Internals;
internal sealed class AwaitableTypes
{
    private readonly INamedTypeSymbol[] _taskOrValueTaskSymbols;

    public AwaitableTypes(Compilation compilation)
    {
        INotifyCompletionSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.INotifyCompletion");
        AsyncMethodBuilderAttributeSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.AsyncMethodBuilderAttribute");
        IAsyncEnumerableSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerable`1");
        IAsyncEnumeratorSymbol = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IAsyncEnumerator`1");
        TaskSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task");
        TaskOfTSymbol = compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1");

        if (INotifyCompletionSymbol is not null)
        {
            var taskLikeSymbols = new List<INamedTypeSymbol>(4);
            taskLikeSymbols.AddIfNotNull(TaskSymbol);
            taskLikeSymbols.AddIfNotNull(TaskOfTSymbol);
            taskLikeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask"));
            taskLikeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1"));
            _taskOrValueTaskSymbols = [.. taskLikeSymbols];
        }
        else
        {
            _taskOrValueTaskSymbols = [];
        }
    }

    private INamedTypeSymbol? TaskSymbol { get; }
    private INamedTypeSymbol? TaskOfTSymbol { get; }
    private INamedTypeSymbol? INotifyCompletionSymbol { get; }
    private INamedTypeSymbol? AsyncMethodBuilderAttributeSymbol { get; }
    public INamedTypeSymbol? IAsyncEnumerableSymbol { get; }
    public INamedTypeSymbol? IAsyncEnumeratorSymbol { get; }

    // https://github.com/dotnet/roslyn/blob/248e85149427c534c4a156a436ecff69bab83b59/src/Compilers/CSharp/Portable/Binder/Binder_Await.cs#L347
    public bool IsAwaitable(ITypeSymbol? symbol, SemanticModel semanticModel, int position)
    {
        if (symbol is null)
            return false;

        if (INotifyCompletionSymbol is null)
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

    public bool IsAwaitable(ITypeSymbol? symbol, Compilation compilation)
    {
        if (symbol is null)
            return false;

        if (INotifyCompletionSymbol is null)
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

            if (!compilation.IsSymbolAccessibleWithin(potentialSymbol, compilation.Assembly))
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
