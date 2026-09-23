using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class EventSourceMustBeSealedFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.EventSourceMustBeSealed);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not TypeDeclarationSyntax nodeToFix)
            return;

        // A class with derived classes cannot be sealed. It should be made abstract instead, which the fixer cannot do
        // safely as the class may be instantiated.
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetDeclaredSymbol(nodeToFix, context.CancellationToken) is not INamedTypeSymbol symbol)
            return;

        var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(symbol, context.Document.Project.Solution, transitive: false, cancellationToken: context.CancellationToken).ConfigureAwait(false);
        if (derivedClasses.Any())
            return;

        var title = "Add sealed modifier";
        var codeAction = CodeAction.Create(
            title,
            ct => AddSealedModifier(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> AddSealedModifier(Document document, TypeDeclarationSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var modifiers = nodeToFix.Modifiers.Add(SyntaxKind.SealedKeyword);
        editor.ReplaceNode(nodeToFix, nodeToFix.WithModifiers(modifiers).WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}
