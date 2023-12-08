using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseDateTimeUnixEpochFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseDateTimeUnixEpoch, RuleIdentifiers.UseDateTimeOffsetUnixEpoch);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        if (context.Diagnostics[0].Id == RuleIdentifiers.UseDateTimeUnixEpoch)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use DateTime.UnixEpoch",
                    ct => Remove(context.Document, nodeToFix, "System.DateTime", ct),
                    equivalenceKey: "Use DateTime.UnixEpoch"),
                context.Diagnostics);
        }
        else
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use DateTimeOffset.UnixEpoch",
                    ct => Remove(context.Document, nodeToFix, "System.DateTimeOffset", ct),
                    equivalenceKey: "Use DateTimeOffset.UnixEpoch"),
                context.Diagnostics);
        }
    }

    private static async Task<Document> Remove(Document document, SyntaxNode node, string type, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var symbol = editor.SemanticModel.Compilation.GetBestTypeByMetadataName(type);
        if (symbol is null)
            return document;

        var generator = editor.Generator;
        var member = generator.MemberAccessExpression(generator.TypeExpression(symbol), "UnixEpoch");
        editor.ReplaceNode(node, member);

        return editor.GetChangedDocument();
    }
}
