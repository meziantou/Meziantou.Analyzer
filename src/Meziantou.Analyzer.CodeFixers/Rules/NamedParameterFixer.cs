using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class NamedParameterFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseNamedParameter);

    public override FixAllProvider GetFixAllProvider() => NamedParameterFixAllProvider.Instance;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (root is null || diagnostic is null)
            return;

        var argumentSpan = diagnostic.Location.SourceSpan;
        var argument = FindArgument(root, argumentSpan);
        if (argument is null || argument.NameColon is not null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (FindParameter(semanticModel, argument, context.CancellationToken) is null)
            return;

        var title = "Add parameter name";
        var codeAction = CodeAction.Create(
            title,
            ct => AddParameterName(context.Document, argumentSpan, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    internal static async Task<Document> AddParameterName(Document document, TextSpan argumentSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;

        var argument = FindArgument(root, argumentSpan);
        if (argument is null || argument.NameColon is not null)
            return document;

        if (FindParameter(semanticModel, argument, cancellationToken) is not { } parameter)
            return document;

        editor.ReplaceNode(argument, argument.WithNameColon(SyntaxFactory.NameColon(parameter.Name)));
        return editor.GetChangedDocument();
    }

    private static ArgumentSyntax? FindArgument(SyntaxNode root, TextSpan argumentSpan)
    {
        // In case the literal is wrapped in an ArgumentSyntax or some other node with the same span,
        // get the innermost node for ties.
        var nodeToFix = root.FindNode(argumentSpan, getInnermostNodeForTie: true);
        return nodeToFix.FirstAncestorOrSelf<ArgumentSyntax>();
    }

    private static IParameterSymbol? FindParameter(SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
    {
        // The argument is bound to its parameter for all the kinds of invocations (methods, constructors, indexers, ...)
        return semanticModel.GetOperation(argument, cancellationToken) is IArgumentOperation { Parameter: { } parameter } ? parameter : null;
    }
}
