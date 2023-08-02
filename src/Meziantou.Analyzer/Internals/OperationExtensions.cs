using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer;

internal static class OperationExtensions
{
#if ROSLYN_4_2_OR_GREATER
    public static IOperation.OperationList GetChildOperations(this IOperation operation)
    {
        return operation.ChildOperations;
    }
#elif ROSLYN_3_8
    public static IEnumerable<IOperation> GetChildOperations(this IOperation operation)
    {
        return operation.Children;
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

    public static bool IsInNameofOperation(this IOperation operation)
    {
        var parent = operation.Parent;
        while (parent != null)
        {
            if (parent.Kind == OperationKind.NameOf)
                return true;

            parent = parent.Parent;
        }

        return false;
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

    public static IOperation UnwrapConversionOperations(this IOperation operation)
    {
        if (operation is IConversionOperation conversionOperation)
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

    public static bool IsInStaticContext(this IOperation operation, CancellationToken cancellationToken) => IsInStaticContext(operation, cancellationToken, out _);
    public static bool IsInStaticContext(this IOperation operation, CancellationToken cancellationToken, out int parentStaticMemberStartPosition)
    {
        // Local functions can be nested, and an instance local function can be declared
        // in a static local function. So, you need to continue to check ancestors when a
        // local function is not static.
        foreach (var member in operation.Syntax.Ancestors())
        {
            if (member is LocalFunctionStatementSyntax localFunction)
            {
                var symbol = operation.SemanticModel!.GetDeclaredSymbol(localFunction, cancellationToken);
                if (symbol != null && symbol.IsStatic)
                {
                    parentStaticMemberStartPosition = localFunction.GetLocation().SourceSpan.Start;
                    return true;
                }
            }
            else if (member is LambdaExpressionSyntax lambdaExpression)
            {
                var symbol = operation.SemanticModel!.GetSymbolInfo(lambdaExpression, cancellationToken).Symbol;
                if (symbol != null && symbol.IsStatic)
                {
                    parentStaticMemberStartPosition = lambdaExpression.GetLocation().SourceSpan.Start;
                    return true;
                }
            }
            else if (member is AnonymousMethodExpressionSyntax anonymousMethod)
            {
                var symbol = operation.SemanticModel!.GetSymbolInfo(anonymousMethod, cancellationToken).Symbol;
                if (symbol != null && symbol.IsStatic)
                {
                    parentStaticMemberStartPosition = anonymousMethod.GetLocation().SourceSpan.Start;
                    return true;
                }
            }
            else if (member is MethodDeclarationSyntax methodDeclaration)
            {
                parentStaticMemberStartPosition = methodDeclaration.GetLocation().SourceSpan.Start;

                var symbol = operation.SemanticModel!.GetDeclaredSymbol(methodDeclaration, cancellationToken);
                return symbol != null && symbol.IsStatic;
            }
        }

        parentStaticMemberStartPosition = -1;
        return false;
    }

    public static IEnumerable<ISymbol> LookupAvailableSymbols(this IOperation operation, CancellationToken cancellationToken)
    {
        // Find available symbols
        var operationLocation = operation.Syntax.GetLocation().SourceSpan.Start;
        var isInStaticContext = operation.IsInStaticContext(cancellationToken, out var parentStaticMemberStartPosition);
        foreach (var symbol in operation.SemanticModel!.LookupSymbols(operationLocation))
        {
            // LookupSymbols check the accessibility of the symbol, but it can
            // suggest instance members when the current context is static.
            if (symbol is IFieldSymbol field && isInStaticContext && !field.IsStatic)
                continue;

            if (symbol is IPropertySymbol { GetMethod: not null } property && isInStaticContext && !property.IsStatic)
                continue;

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
                    isValid &= IsValid(location, operationLocation, isInStaticContext ? parentStaticMemberStartPosition : null);
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
            }


            yield return symbol;
        }
    }
}
