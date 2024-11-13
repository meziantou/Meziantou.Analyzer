using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStringComparisonFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringComparison, RuleIdentifiers.AvoidCultureSensitiveMethod);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // In case the ArrayCreationExpressionSyntax is wrapped in an ArgumentSyntax or some other node with the same span,
        // get the innermost node for ties.
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        AddCodeFix(nameof(StringComparison.Ordinal));
        AddCodeFix(nameof(StringComparison.OrdinalIgnoreCase));

        void AddCodeFix(string comparisonMode)
        {
            var title = "Add StringComparison." + comparisonMode;
            var codeAction = CodeAction.Create(
                title,
                ct => AddStringComparison(context.Document, nodeToFix, comparisonMode, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> AddStringComparison(Document document, SyntaxNode nodeToFix, string stringComparisonMode, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        var invocationExpression = (InvocationExpressionSyntax)nodeToFix;
        if (invocationExpression is null)
            return document;

        var stringComparison = semanticModel.Compilation.GetBestTypeByMetadataName("System.StringComparison");
        if (stringComparison is null)
            return document;

        var newArgument = (ArgumentSyntax)generator.Argument(
            generator.MemberAccessExpression(
                generator.TypeExpression(stringComparison, addImport: true),
                stringComparisonMode));

        editor.ReplaceNode(invocationExpression, invocationExpression.AddArgumentListArguments(newArgument));
        return editor.GetChangedDocument();
    }
}
