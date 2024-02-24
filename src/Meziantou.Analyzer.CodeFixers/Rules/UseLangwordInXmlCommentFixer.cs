using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseLangwordInXmlCommentFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseLangwordInXmlComment);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true, findInsideTrivia: true);
        if (nodeToFix is null)
            return;

        if (!context.Diagnostics[0].Properties.TryGetValue("keyword", out var keyword) || keyword is null)
            return;

        var title = $"Use <see langword=\"{keyword}\" />";
        var codeAction = CodeAction.Create(
            title,
            cancellationToken => Fix(context.Document, nodeToFix, keyword, cancellationToken),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, SyntaxNode nodeToFix, string keyword, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newNode = XmlNullKeywordElement()
                .WithLessThanToken(Token(SyntaxKind.LessThanToken))
                .WithAttributes(
                    SingletonList<XmlAttributeSyntax>(
                        XmlTextAttribute("langword", keyword)));

        editor.ReplaceNode(nodeToFix, newNode);
        return editor.GetChangedDocument();
    }
}
