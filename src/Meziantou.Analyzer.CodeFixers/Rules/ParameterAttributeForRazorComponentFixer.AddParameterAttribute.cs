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
public sealed class ParameterAttributeForRazorComponentFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
        RuleIdentifiers.SupplyParameterFromQueryRequiresParameterAttributeForRazorComponent,
        RuleIdentifiers.EditorRequiredRequiresParameterAttributeForRazorComponent);

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

        context.RegisterCodeFix(CodeAction.Create("Add [Parameter]", ct => AddAttribute(context.Document, nodeToFix, ct), equivalenceKey: "Add [Parameter]"), context.Diagnostics);
    }

    private static async Task<Document> AddAttribute(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var newNode = generator.AddAttributes(nodeToFix, generator.Attribute("Microsoft.AspNetCore.Components.ParameterAttribute"));
        editor.ReplaceNode(nodeToFix, newNode);
        return editor.GetChangedDocument();
    }
}
