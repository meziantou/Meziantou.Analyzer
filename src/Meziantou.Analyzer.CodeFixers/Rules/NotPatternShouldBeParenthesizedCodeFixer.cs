using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class NotPatternShouldBeParenthesizedCodeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.NotPatternShouldBeParenthesized);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        {
            var title = "Add parentheses around not";
            var codeAction = CodeAction.Create(
                title,
                ct => ParenthesizeNotPattern(context.Document, nodeToFix, ct),
                equivalenceKey: title);
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        {
            var title = "Negate all or patterns";
            var codeAction = CodeAction.Create(
                title,
                ct => ParenthesizeOrPattern(context.Document, nodeToFix, ct),
                equivalenceKey: title);
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> ParenthesizeNotPattern(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToFix, SyntaxFactory.ParenthesizedPattern((PatternSyntax)nodeToFix));
        return editor.GetChangedDocument();
    }

    private static async Task<Document> ParenthesizeOrPattern(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        if (nodeToFix is not UnaryPatternSyntax unary)
            return document;

        var root = unary.Ancestors().TakeWhile(IsOrPattern).LastOrDefault();
        if (root is null)
            return document;

        editor.ReplaceNode(root, SyntaxFactory.UnaryPattern(SyntaxFactory.Token(SyntaxKind.NotKeyword), SyntaxFactory.ParenthesizedPattern((PatternSyntax)root.ReplaceNode(unary, unary.Pattern))));

        return editor.GetChangedDocument();
    }

    private static bool IsOrPattern(SyntaxNode? node) => node is BinaryPatternSyntax binaryPatternSyntax && binaryPatternSyntax.OperatorToken.IsKind(SyntaxKind.OrKeyword);
}
