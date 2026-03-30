using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStringEqualsInsteadOfIsPatternFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringEqualsInsteadOfIsPattern);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var isPatternExpression = nodeToFix as IsPatternExpressionSyntax ?? nodeToFix.AncestorsAndSelf().OfType<IsPatternExpressionSyntax>().FirstOrDefault();
        if (isPatternExpression is null)
            return;

        if (isPatternExpression.Pattern is not ConstantPatternSyntax { Expression: ExpressionSyntax constantExpression })
            return;

        RegisterCodeFix(nameof(StringComparison.Ordinal));
        RegisterCodeFix(nameof(StringComparison.OrdinalIgnoreCase));

        void RegisterCodeFix(string comparisonMode)
        {
            var title = "Use string.Equals " + comparisonMode;
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    ct => ReplaceWithStringEquals(context.Document, isPatternExpression, constantExpression, comparisonMode, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
    }

    private static async Task<Document> ReplaceWithStringEquals(Document document, IsPatternExpressionSyntax isPatternExpression, ExpressionSyntax constantExpression, string comparisonMode, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var stringComparisonType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName("System.StringComparison");
        if (stringComparisonType is null)
            return document;

        var newExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(generator.TypeExpression(SpecialType.System_String), nameof(string.Equals)),
            isPatternExpression.Expression,
            constantExpression,
            generator.MemberAccessExpression(generator.TypeExpression(stringComparisonType, addImport: true), comparisonMode));

        editor.ReplaceNode(isPatternExpression, newExpression.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}
