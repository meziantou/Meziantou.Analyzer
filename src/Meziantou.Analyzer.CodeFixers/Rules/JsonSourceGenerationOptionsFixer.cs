using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class JsonSourceGenerationOptionsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.SetRespectNullableAnnotations, RuleIdentifiers.SetRespectRequiredConstructorParameters);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not (AttributeSyntax or TypeDeclarationSyntax))
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var attributeSymbol = semanticModel.Compilation.GetBestTypeByMetadataName("System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute");
        if (attributeSymbol is null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var propertyName = GetPropertyName(diagnostic.Id);
            if (propertyName is null)
                continue;

            var title = $"Set {propertyName} to true";
            context.RegisterCodeFix(
                CodeAction.Create(title, ct => Refactor(context.Document, nodeToFix, propertyName, attributeSymbol, ct), equivalenceKey: title),
                diagnostic);
        }
    }

    private static string? GetPropertyName(string diagnosticId) => diagnosticId switch
    {
        RuleIdentifiers.SetRespectNullableAnnotations => "RespectNullableAnnotations",
        RuleIdentifiers.SetRespectRequiredConstructorParameters => "RespectRequiredConstructorParameters",
        _ => null,
    };

    private static async Task<Document> Refactor(Document document, SyntaxNode nodeToFix, string propertyName, INamedTypeSymbol attributeSymbol, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        if (nodeToFix is AttributeSyntax attribute)
        {
            var argument = SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals(propertyName), nameColon: null, SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));
            var argumentList = attribute.ArgumentList ?? SyntaxFactory.AttributeArgumentList();
            editor.ReplaceNode(attribute, attribute.WithArgumentList(argumentList.AddArguments(argument)));
        }
        else
        {
            // The context has no [JsonSourceGenerationOptions] attribute, so the fix adds it
            var generator = editor.Generator;
            var newAttribute = generator.Attribute(
                generator.TypeExpression(attributeSymbol).WithAdditionalAnnotations(Simplifier.AddImportsAnnotation),
                [generator.AttributeArgument(propertyName, generator.TrueLiteralExpression())]);

            editor.AddAttribute(nodeToFix, newAttribute);
        }

        return editor.GetChangedDocument();
    }
}
