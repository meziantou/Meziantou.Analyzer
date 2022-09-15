using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer;

internal static class OperationExtensions
{
#if ROSLYN3
    public static IEnumerable<IOperation> GetChildOperations(this IOperation operation)
    {
        return operation.Children;
    }
#elif ROSLYN4
    public static IOperation.OperationList GetChildOperations(this IOperation operation)
    {
        return operation.ChildOperations;
    }
#endif

    public static LanguageVersion GetCSharpLanguageVersion(this IOperation operation)
    {
        if (operation.Syntax.SyntaxTree.Options is CSharpParseOptions options)
            return options.LanguageVersion;

        return LanguageVersion.Default;
    }
    
    public static LanguageVersion GetCSharpLanguageVersion(this SyntaxNode syntaxNode)
    {
        if (syntaxNode.SyntaxTree.Options is CSharpParseOptions options)
            return options.LanguageVersion;

        return LanguageVersion.Default;
    }
    
    public static LanguageVersion GetCSharpLanguageVersion(this SyntaxTree syntaxTree)
    {
        if (syntaxTree.Options is CSharpParseOptions options)
            return options.LanguageVersion;

        return LanguageVersion.Default;
    }

    public static LanguageVersion GetCSharpLanguageVersion(this Compilation compilation)
    {
        var syntaxTree = compilation.SyntaxTrees.FirstOrDefault();
        if (syntaxTree?.Options is CSharpParseOptions options)
            return options.LanguageVersion;

        return LanguageVersion.Default;
    }

    public static IEnumerable<IOperation> Ancestors(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent != null)
        {
            yield return parent;
            parent = parent.Parent;
        }
    }

    public static bool IsInQueryableExpressionArgument(this IOperation operation)
    {
        var semanticModel = operation.SemanticModel;
        if (semanticModel == null)
            return false;

        foreach (var invocationOperation in operation.Ancestors().OfType<IInvocationOperation>())
        {
            var type = invocationOperation.TargetMethod.ContainingType;
            if (type.IsEqualTo(semanticModel.Compilation.GetBestTypeByMetadataName("System.Linq.Queryable")))
                return true;
        }

        return false;
    }

    public static bool IsInExpressionContext(this IOperation operation)
    {
        var semanticModel = operation.SemanticModel;
        if (semanticModel == null)
            return false;

        foreach (var op in operation.Ancestors())
        {
            if (op is IArgumentOperation argumentOperation)
            {
                if (argumentOperation.Parameter == null)
                    continue;

                var type = argumentOperation.Parameter.Type;
                if (type.InheritsFrom(semanticModel.Compilation.GetBestTypeByMetadataName("System.Linq.Expressions.Expression")))
                    return true;
            }
            else if (op is IConversionOperation conversionOperation)
            {
                var type = conversionOperation.Type;
                if (type is null)
                    continue;

                if (type.InheritsFrom(semanticModel.Compilation.GetBestTypeByMetadataName("System.Linq.Expressions.Expression")))
                    return true;
            }
        }

        return false;
    }

    public static bool IsInNameofOperation(this IOperation operation)
    {
        return operation.Ancestors().OfType<INameOfOperation>().Any();
    }

    public static ITypeSymbol? GetActualType(this IOperation operation)
    {
        if (operation is IConversionOperation conversionOperation)
        {
            return GetActualType(conversionOperation.Operand);
        }

        return operation.Type;
    }

    public static IOperation UnwrapImplicitConversionOperations(this IOperation operation)
    {
        if (operation is IConversionOperation conversionOperation && conversionOperation.IsImplicit)
        {
            return UnwrapImplicitConversionOperations(conversionOperation.Operand);
        }

        return operation;
    }

    public static bool HasArgumentOfType(this IInvocationOperation operation, ITypeSymbol argumentTypeSymbol)
    {
        foreach (var arg in operation.Arguments)
        {
            if (argumentTypeSymbol.IsEqualTo(arg.Value.Type))
                return true;
        }

        return false;
    }

    public static IMethodSymbol? GetContainingMethod(this IOperation operation, CancellationToken cancellationToken)
    {
        if (operation.SemanticModel == null)
            return null;

        foreach (var syntax in operation.Syntax.AncestorsAndSelf())
        {
            if (syntax is MethodDeclarationSyntax method)
                return operation.SemanticModel.GetDeclaredSymbol(method, cancellationToken);
        }

        return null;
    }
}
