namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MarkAttributesWithAttributeUsageAttributeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MarkAttributesWithAttributeUsageAttribute);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        // The type is a class, or a record that does not compile as it derives from Attribute (CS8864).
        // The type has no name when its identifier is missing, and the diagnostic is then reported on another node: class : Attribute
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not TypeDeclarationSyntax typeDeclaration)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var attributeUsageAttribute = semanticModel.Compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
        if (attributeUsageAttribute is null)
            return;

        var attributeTargets = semanticModel.Compilation.GetTypeByMetadataName("System.AttributeTargets");
        if (attributeTargets is null)
            return;

        var title = "Add AttributeUsage attribute";
        var codeAction = CodeAction.Create(
            title,
            ct => Refactor(context.Document, typeDeclaration, attributeUsageAttribute, attributeTargets, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Refactor(Document document, TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol attributeUsageAttribute, INamedTypeSymbol attributeTargets, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var attribute = editor.Generator.Attribute(
            generator.TypeExpression(attributeUsageAttribute, addImport: true),
            [
                    generator.AttributeArgument(
                        generator.TypeMemberAccessExpression(attributeTargets, nameof(AttributeTargets.All), addImport: true)),
            ]);

        editor.AddAttribute(typeDeclaration, attribute);
        return editor.GetChangedDocument();
    }
}
