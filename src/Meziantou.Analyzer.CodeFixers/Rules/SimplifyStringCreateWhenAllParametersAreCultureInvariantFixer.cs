using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class SimplifyStringCreateWhenAllParametersAreCultureInvariantFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.SimplifyStringCreateWhenAllParametersAreCultureInvariant);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not InvocationExpressionSyntax nodeToFix)
            return;

        var title = "Simplify to interpolated string";
        var codeAction = CodeAction.Create(
            title,
            ct => Fix(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, InvocationExpressionSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        if (editor.SemanticModel.GetOperation(nodeToFix, cancellationToken) is not IInvocationOperation op)
            return document;

        // Get the interpolated string from the second argument
        if (op.Arguments.Length != 2)
            return document;

        var interpolatedStringArgument = op.Arguments[1];

#if CSHARP10_OR_GREATER
        if (interpolatedStringArgument.Value is IInterpolatedStringHandlerCreationOperation handlerCreation)
        {
            // The Content property contains the interpolated string operation
            if (handlerCreation.Content is IInterpolatedStringOperation interpolatedString)
            {
                // Get the syntax of the interpolated string
                var interpolatedStringSyntax = interpolatedString.Syntax as InterpolatedStringExpressionSyntax;
                if (interpolatedStringSyntax is not null)
                {
                    editor.ReplaceNode(nodeToFix, interpolatedStringSyntax);
                }
            }
        }
#endif

        return editor.GetChangedDocument();
    }
}
