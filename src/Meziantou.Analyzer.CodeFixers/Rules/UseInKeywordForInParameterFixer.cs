using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseInKeywordForInParameterFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseInKeywordForInParameter, RuleIdentifiers.UseInKeywordToSelectInOverload);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (root is null || diagnostic is null)
            return;

        if (root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<ArgumentSyntax>() is not { } argument)
            return;

        if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetOperation(argument, context.CancellationToken) is not IArgumentOperation operation)
            return;

        if (!UseInKeywordForInParameterCommon.CanBePassedByReference(operation.Value))
            return;

        var title = "Add in keyword";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                cancellationToken => AddInKeywordAsync(context.Document, diagnostic.Location.SourceSpan, diagnostic.Properties.GetValueOrDefault(OverloadFinder.NamespaceToImportPropertyName), cancellationToken),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> AddInKeywordAsync(Document document, TextSpan argumentSpan, string? namespaceToImport, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        if (root.FindNode(argumentSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<ArgumentSyntax>() is not { } argument)
            return document;

        if (!argument.RefKindKeyword.IsKind(SyntaxKind.None))
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(argument, argument.WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.InKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
        if (namespaceToImport is not null)
        {
            UsingDirectiveHelper.AddUsingDirective(editor, argument, namespaceToImport);
        }

        return editor.GetChangedDocument();
    }
}
