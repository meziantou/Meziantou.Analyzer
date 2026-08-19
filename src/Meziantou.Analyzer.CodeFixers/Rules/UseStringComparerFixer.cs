using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Meziantou.Framework.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStringComparerFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringComparer);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // In case the target expression is wrapped in another node with the same span,
        // get the innermost node for ties.
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true)
            ?.FirstAncestorOrSelf<SyntaxNode>(CanFix);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var stringComparerSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.StringComparer");
        if (stringComparerSymbol is null)
            return;

        var equalityComparerOpenType = semanticModel.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
        var comparerOpenType = semanticModel.Compilation.GetBestTypeByMetadataName("System.Collections.Generic.IComparer`1");

        var insertionIndex = -1;
        var parameterName = string.Empty;

        var operation = semanticModel.GetOperation(nodeToFix, context.CancellationToken);
        IMethodSymbol? currentMethod = operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IObjectCreationOperation creation => creation.Constructor,
            _ => null,
        };

        if (currentMethod is not null && (equalityComparerOpenType is not null || comparerOpenType is not null))
        {
            var overloadFinder = new OverloadFinder(semanticModel.Compilation);
            var equalityComparerStringType = GetIEqualityComparerString(semanticModel.Compilation);
            var comparerStringType = GetIComparerString(semanticModel.Compilation);

            IMethodSymbol? targetOverload = null;
            if (equalityComparerStringType is not null)
            {
                targetOverload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
                    currentMethod, new OverloadOptions { SyntaxNode = nodeToFix }, [equalityComparerStringType]);
            }

            if (targetOverload is null && comparerStringType is not null)
            {
                targetOverload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
                    currentMethod, new OverloadOptions { SyntaxNode = nodeToFix }, [comparerStringType]);
            }

            if (targetOverload is not null)
            {
                TryGetComparerParameterInfo(currentMethod, targetOverload, equalityComparerOpenType, comparerOpenType, out insertionIndex, out parameterName);
            }
        }

        // When the argument list uses named arguments, the comparer must be named too. Without the
        // name of the parameter, the fix would generate code that doesn't compile.
        if (parameterName.Length == 0 && GetArguments(nodeToFix).Any(argument => argument.NameColon is not null))
            return;

        RegisterCodeFix(nameof(StringComparer.Ordinal));
        RegisterCodeFix(nameof(StringComparer.OrdinalIgnoreCase));

        void RegisterCodeFix(string comparerName)
        {
            var title = "Add StringComparer." + comparerName;
            var codeAction = CodeAction.Create(
                title,
                ct => AddStringComparer(context.Document, nodeToFix, comparerName, stringComparerSymbol, insertionIndex, parameterName, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static bool TryGetComparerParameterInfo(IMethodSymbol method, IMethodSymbol overload, INamedTypeSymbol? equalityComparerOpenType, INamedTypeSymbol? comparerOpenType, out int insertionIndex, out string parameterName)
    {
        // Use comparable parameters to correctly handle extension methods:
        // strip the implicit 'this' parameter so indices align with the argument list.
        var methodParams = GetComparableParameters(method);
        var overloadParams = GetComparableParameters(overload);

        for (var i = 0; i < overloadParams.Length; i++)
        {
            var parameter = overloadParams[i];
            var originalDef = parameter.Type.OriginalDefinition;
            if ((equalityComparerOpenType is null || !originalDef.IsEqualTo(equalityComparerOpenType)) &&
                (comparerOpenType is null || !originalDef.IsEqualTo(comparerOpenType)))
            {
                continue;
            }

            if (i >= methodParams.Length || !IsComparerType(methodParams[i].Type, equalityComparerOpenType, comparerOpenType))
            {
                insertionIndex = i;
                parameterName = parameter.Name;
                return true;
            }
        }

        insertionIndex = -1;
        parameterName = string.Empty;
        return false;
    }

    /// <summary>
    /// Returns the parameters that correspond to actual arguments in the invocation/creation argument list,
    /// i.e. excluding the implicit 'this' receiver for extension methods.
    /// </summary>
    private static ImmutableArray<IParameterSymbol> GetComparableParameters(IMethodSymbol method)
    {
        // Reduced extension method: Parameters already exclude 'this'.
        if (method.MethodKind is MethodKind.ReducedExtension)
            return method.Parameters;

        // Non-reduced extension method: the first parameter is 'this' and is not an argument.
        if (method.IsExtensionMethod && method.Parameters.Length > 0)
            return method.Parameters.RemoveAt(0);

        return method.Parameters;
    }

    private static bool IsComparerType(ITypeSymbol type, INamedTypeSymbol? equalityComparerOpenType, INamedTypeSymbol? comparerOpenType)
    {
        var originalDef = type.OriginalDefinition;
        return (equalityComparerOpenType is not null && originalDef.IsEqualTo(equalityComparerOpenType)) ||
               (comparerOpenType is not null && originalDef.IsEqualTo(comparerOpenType));
    }

    private static INamedTypeSymbol? GetIEqualityComparerString(Compilation compilation)
    {
        var openType = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IEqualityComparer`1");
        if (openType is null)
            return null;

        return openType.Construct(compilation.GetSpecialType(SpecialType.System_String));
    }

    private static INamedTypeSymbol? GetIComparerString(Compilation compilation)
    {
        var openType = compilation.GetBestTypeByMetadataName("System.Collections.Generic.IComparer`1");
        if (openType is null)
            return null;

        return openType.Construct(compilation.GetSpecialType(SpecialType.System_String));
    }

    private static async Task<Document> AddStringComparer(Document document, SyntaxNode nodeToFix, string comparerName, INamedTypeSymbol stringComparer, int insertionIndex, string parameterName, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var comparerExpression = generator.MemberAccessExpression(
            generator.TypeExpression(stringComparer, addImport: true),
            comparerName);

        switch (nodeToFix)
        {
            case ObjectCreationExpressionSyntax creationExpression:
                editor.ReplaceNode(creationExpression, AddArgument(creationExpression, comparerExpression, insertionIndex, parameterName, generator));
                break;

            case ImplicitObjectCreationExpressionSyntax implicitCreationExpression:
            {
                var newArguments = AddArgument(implicitCreationExpression.ArgumentList.Arguments, comparerExpression, insertionIndex, parameterName, generator);
                editor.ReplaceNode(implicitCreationExpression, implicitCreationExpression.WithArgumentList(implicitCreationExpression.ArgumentList.WithArguments(newArguments)));
                break;
            }

            case InvocationExpressionSyntax invocationExpression:
            {
                var newArguments = AddArgument(invocationExpression.ArgumentList.Arguments, comparerExpression, insertionIndex, parameterName, generator);
                editor.ReplaceNode(invocationExpression, invocationExpression.WithArgumentList(invocationExpression.ArgumentList.WithArguments(newArguments)));
                break;
            }

#if CSHARP15_OR_GREATER
            case CollectionExpressionSyntax collectionExpression:
                editor.ReplaceNode(collectionExpression, AddCollectionArgument(collectionExpression, stringComparer, comparerName));
                break;
#endif

            default:
                return document;
        }

        return editor.GetChangedDocument();
    }

    private static ObjectCreationExpressionSyntax AddArgument(ObjectCreationExpressionSyntax creationExpression, SyntaxNode comparerExpression, int insertionIndex, string parameterName, SyntaxGenerator generator)
    {
        if (creationExpression.ArgumentList is not null)
        {
            var newArguments = AddArgument(creationExpression.ArgumentList.Arguments, comparerExpression, insertionIndex, parameterName, generator);
            return creationExpression.WithArgumentList(creationExpression.ArgumentList.WithArguments(newArguments));
        }

        var trailingTrivia = creationExpression.Type.GetTrailingTrivia();
        var newArgument = (ArgumentSyntax)generator.Argument(comparerExpression);
        return creationExpression
            .WithType(creationExpression.Type.WithoutTrailingTrivia())
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(newArgument)).WithTrailingTrivia(trailingTrivia));
    }

    private static SeparatedSyntaxList<ArgumentSyntax> AddArgument(SeparatedSyntaxList<ArgumentSyntax> arguments, SyntaxNode comparerExpression, int insertionIndex, string parameterName, SyntaxGenerator generator)
    {
        // The comparer must be named when it cannot be added at its own position, or when the argument list
        // already contains named arguments as adding an unnamed argument may not compile (CS8323).
        var useNamedArgument = insertionIndex > arguments.Count || arguments.Any(argument => argument.NameColon is not null);
        var newArgument = useNamedArgument
            ? (ArgumentSyntax)generator.Argument(parameterName, RefKind.None, comparerExpression)
            : (ArgumentSyntax)generator.Argument(comparerExpression);

        return insertionIndex >= 0 && insertionIndex < arguments.Count
            ? arguments.Insert(insertionIndex, newArgument)
            : arguments.Add(newArgument);
    }

    private static SeparatedSyntaxList<ArgumentSyntax> GetArguments(SyntaxNode nodeToFix)
    {
        return nodeToFix switch
        {
            ObjectCreationExpressionSyntax creationExpression => creationExpression.ArgumentList?.Arguments ?? default,
            ImplicitObjectCreationExpressionSyntax implicitCreationExpression => implicitCreationExpression.ArgumentList.Arguments,
            InvocationExpressionSyntax invocationExpression => invocationExpression.ArgumentList.Arguments,
            _ => default,
        };
    }

    private static bool CanFix(SyntaxNode nodeToFix)
    {
        return nodeToFix is ObjectCreationExpressionSyntax
            or ImplicitObjectCreationExpressionSyntax
            or InvocationExpressionSyntax
#if CSHARP15_OR_GREATER
            or CollectionExpressionSyntax
#endif
            ;
    }

#if CSHARP15_OR_GREATER
    private static CollectionExpressionSyntax AddCollectionArgument(CollectionExpressionSyntax collectionExpression, INamedTypeSymbol stringComparer, string comparerName)
    {
        var comparerType = stringComparer.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var parsed = (CollectionExpressionSyntax)SyntaxFactory.ParseExpression($"[with({comparerType}.{comparerName})]");
        var withElement = parsed.Elements[0].WithAdditionalAnnotations(Simplifier.Annotation);
        var newElements = collectionExpression.Elements.Insert(0, withElement);
        return collectionExpression.WithElements(newElements);
    }
#endif
}
