using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class PreserveParamsOnOverrideFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.PreserveParamsOnOverride);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not ParameterSyntax parameterSyntax)
            return;

        var title = "Add params";
        var codeAction = CodeAction.Create(
            title,
            ct => AddParams(context.Document, parameterSyntax, ct),
            equivalenceKey: title);
        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> AddParams(Document document, ParameterSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToFix, nodeToFix.AddModifiers(SyntaxFactory.Token(SyntaxKind.ParamsKeyword)));
        return editor.GetChangedDocument();
    }
}
