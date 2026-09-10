using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseTypeofInsteadOfGetTypeOnSealedTypeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseTypeofInsteadOfGetTypeOnSealedType);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.GetOperation(nodeToFix, context.CancellationToken) is not IInvocationOperation operation)
            return;

        if (UseTypeofInsteadOfGetTypeOnSealedTypeCommon.GetKnownType(operation) is not { } type)
            return;

        var title = $"Use 'typeof({type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)})'";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => UseTypeofAsync(context.Document, nodeToFix, type, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> UseTypeofAsync(Document document, SyntaxNode nodeToFix, INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var typeExpression = generator.TypeExpression(type, addImport: true).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation);
        var newNode = generator.TypeOfExpression(typeExpression)
            .WithTriviaFrom(nodeToFix)
            .WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);

        editor.ReplaceNode(nodeToFix, newNode);
        return editor.GetChangedDocument();
    }
}
