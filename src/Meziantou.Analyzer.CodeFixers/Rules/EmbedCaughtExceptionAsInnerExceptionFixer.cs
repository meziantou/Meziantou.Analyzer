using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class EmbedCaughtExceptionAsInnerExceptionFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.EmbedCaughtExceptionAsInnerException);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true)?.FirstAncestorOrSelf<BaseObjectCreationExpressionSyntax>();
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(nodeToFix, context.CancellationToken) is not IObjectCreationOperation objectCreationOperation)
            return;

        var exceptionSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.Exception");
        if (exceptionSymbol is null || objectCreationOperation.Constructor is null)
            return;

        var catchClause = nodeToFix.FirstAncestorOrSelf<CatchClauseSyntax>();
        if (catchClause?.Declaration?.Identifier.ValueText is not { Length: > 0 } exceptionIdentifier)
            return;

        var catchVariableSymbol = semanticModel.GetDeclaredSymbol(catchClause.Declaration, context.CancellationToken) as ILocalSymbol;
        if (catchVariableSymbol?.Type.IsOrInheritFrom(exceptionSymbol) is not true)
            return;

        var overloadFinder = new OverloadFinder(semanticModel.Compilation);
        var overload = overloadFinder.FindOverloadWithAdditionalParameterOfType(
            objectCreationOperation,
            new OverloadOptions(IncludeObsoleteMembers: false, AllowOptionalParameters: false),
            [exceptionSymbol]);
        if (overload is null || !TryGetExceptionParameterInfo(objectCreationOperation.Constructor, overload, exceptionSymbol, out var insertionIndex, out var parameterName))
            return;

        var title = "Add caught exception as innerException";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => AddInnerException(context.Document, nodeToFix, exceptionIdentifier, insertionIndex, parameterName, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> AddInnerException(Document document, BaseObjectCreationExpressionSyntax objectCreationExpression, string exceptionIdentifier, int insertionIndex, string parameterName, CancellationToken cancellationToken)
    {
        if (objectCreationExpression.ArgumentList is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var exceptionArgumentExpression = SyntaxFactory.IdentifierName(exceptionIdentifier);
        var currentArguments = objectCreationExpression.ArgumentList.Arguments;
        var newArguments = insertionIndex > currentArguments.Count
            ? currentArguments.Add((ArgumentSyntax)generator.Argument(parameterName, RefKind.None, exceptionArgumentExpression))
            : currentArguments.Insert(insertionIndex, (ArgumentSyntax)generator.Argument(exceptionArgumentExpression));

        var newArgumentList = objectCreationExpression.ArgumentList.WithArguments(newArguments);
        var updatedNode = objectCreationExpression switch
        {
            ObjectCreationExpressionSyntax objectCreation => objectCreation.WithArgumentList(newArgumentList),
            ImplicitObjectCreationExpressionSyntax implicitObjectCreation => implicitObjectCreation.WithArgumentList(newArgumentList),
            _ => objectCreationExpression,
        };

        editor.ReplaceNode(objectCreationExpression, updatedNode);
        return editor.GetChangedDocument();
    }

    private static bool TryGetExceptionParameterInfo(IMethodSymbol method, IMethodSymbol overload, ITypeSymbol exceptionSymbol, out int insertionIndex, out string parameterName)
    {
        for (var i = 0; i < overload.Parameters.Length; i++)
        {
            var parameter = overload.Parameters[i];
            if (!parameter.Type.IsOrInheritFrom(exceptionSymbol))
                continue;

            if (i >= method.Parameters.Length || !method.Parameters[i].Type.IsOrInheritFrom(exceptionSymbol))
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
}
