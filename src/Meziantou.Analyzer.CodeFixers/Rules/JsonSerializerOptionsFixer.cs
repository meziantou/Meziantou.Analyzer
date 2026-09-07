using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class JsonSerializerOptionsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.SetRespectNullableAnnotationsOnJsonSerializerOptions, RuleIdentifiers.SetRespectRequiredConstructorParametersOnJsonSerializerOptions);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root?.FindNode(context.Span, getInnermostNodeForTie: true) is not BaseObjectCreationExpressionSyntax creation)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var propertyName = GetPropertyName(diagnostic.Id);
            if (propertyName is null)
                continue;

            var title = $"Set {propertyName} to true";
            context.RegisterCodeFix(
                CodeAction.Create(title, ct => Refactor(context.Document, creation, propertyName, ct), equivalenceKey: title),
                diagnostic);
        }
    }

    private static string? GetPropertyName(string diagnosticId) => diagnosticId switch
    {
        RuleIdentifiers.SetRespectNullableAnnotationsOnJsonSerializerOptions => "RespectNullableAnnotations",
        RuleIdentifiers.SetRespectRequiredConstructorParametersOnJsonSerializerOptions => "RespectRequiredConstructorParameters",
        _ => null,
    };

    private static async Task<Document> Refactor(Document document, BaseObjectCreationExpressionSyntax creation, string propertyName, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var assignment = SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(propertyName),
            SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression));

        var initializer = creation.Initializer ?? SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression);
        editor.ReplaceNode(creation, creation.WithInitializer(initializer.AddExpressions(assignment)).WithAdditionalAnnotations(Formatter.Annotation));

        return editor.GetChangedDocument();
    }
}
