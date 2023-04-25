using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Meziantou.Analyzer.Internals;
internal sealed class AwaitableTypes
{
    private readonly INamedTypeSymbol[] _taskLikeSymbols;

    public AwaitableTypes(Compilation compilation)
    {
        INotifyCompletionSymbol = compilation.GetBestTypeByMetadataName("System.Runtime.CompilerServices.INotifyCompletion");

        if (INotifyCompletionSymbol != null)
        {
            var taskLikeSymbols = new List<INamedTypeSymbol>(4);
            taskLikeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task"));
            taskLikeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Threading.Tasks.Task`1"));
            taskLikeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask"));
            taskLikeSymbols.AddIfNotNull(compilation.GetBestTypeByMetadataName("System.Threading.Tasks.ValueTask`1"));
            _taskLikeSymbols = taskLikeSymbols.ToArray();
        }
        else
        {
            _taskLikeSymbols = Array.Empty<INamedTypeSymbol>();
        }
    }

    private INamedTypeSymbol? INotifyCompletionSymbol { get; }

    // https://github.com/dotnet/roslyn/blob/248e85149427c534c4a156a436ecff69bab83b59/src/Compilers/CSharp/Portable/Binder/Binder_Await.cs#L347
    public bool IsAwaitable(ITypeSymbol? symbol, SemanticModel semanticModel, int position)
    {
        if (symbol == null)
            return false;

        if (INotifyCompletionSymbol == null)
            return false;

        if (symbol.SpecialType is SpecialType.System_Void || symbol.TypeKind is TypeKind.Dynamic)
            return false;

        if (IsTaskLike(symbol))
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

    private bool IsTaskLike(ITypeSymbol? symbol)
    {
        if (symbol is null)
            return false;

        var originalDefinition = symbol.OriginalDefinition;
        foreach (var taskLikeSymbol in _taskLikeSymbols)
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
