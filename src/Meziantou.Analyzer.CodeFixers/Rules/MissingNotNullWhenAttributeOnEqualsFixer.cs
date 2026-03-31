using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MissingNotNullWhenAttributeOnEqualsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MissingNotNullWhenAttributeOnEquals);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        var parameter = nodeToFix?.FirstAncestorOrSelf<ParameterSyntax>();
        if (parameter is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetDeclaredSymbol(parameter, context.CancellationToken) is not IParameterSymbol parameterSymbol)
            return;

        if (parameterSymbol.ContainingSymbol is not IMethodSymbol methodSymbol)
            return;

        var (attributeMetadataName, expectedValue, title) = methodSymbol.Name switch
        {
            "TryGetValue" when parameterSymbol.RefKind == RefKind.Out => ("System.Diagnostics.CodeAnalysis.MaybeNullWhenAttribute", false, "Add [MaybeNullWhen(false)]"),
            "Equals" => ("System.Diagnostics.CodeAnalysis.NotNullWhenAttribute", true, "Add [NotNullWhen(true)]"),
            _ => default,
        };

        if (attributeMetadataName is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => AddOrUpdateAttribute(context.Document, parameter, attributeMetadataName, expectedValue, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> AddOrUpdateAttribute(Document document, ParameterSyntax parameter, string attributeMetadataName, bool expectedValue, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        var attributeSymbol = semanticModel.Compilation.GetBestTypeByMetadataName(attributeMetadataName);
        if (attributeSymbol is null)
            return document;

        var boolExpression = expectedValue ? LiteralExpression(SyntaxKind.TrueLiteralExpression) : LiteralExpression(SyntaxKind.FalseLiteralExpression);
        var argumentList = AttributeArgumentList(SingletonSeparatedList(AttributeArgument(boolExpression)));

        var existingAttribute = parameter.AttributeLists
            .SelectMany(static attributeList => attributeList.Attributes)
            .FirstOrDefault(attribute =>
            {
                var symbol = semanticModel.GetSymbolInfo(attribute, cancellationToken).Symbol?.ContainingType;
                return symbol.IsEqualTo(attributeSymbol);
            });

        if (existingAttribute is not null)
        {
            editor.ReplaceNode(existingAttribute, existingAttribute.WithArgumentList(argumentList));
            return editor.GetChangedDocument();
        }

        var newAttribute = generator.Attribute(
            generator.TypeExpression(attributeSymbol, addImport: true),
            [
                generator.AttributeArgument(generator.LiteralExpression(expectedValue)),
            ]);

        var newNode = generator.AddAttributes(parameter, newAttribute);
        editor.ReplaceNode(parameter, newNode);
        return editor.GetChangedDocument();
    }
}
