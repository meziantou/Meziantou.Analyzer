using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseInlineArrayInsteadOfFixedBufferFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseInlineArrayInsteadOfFixedBuffer);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix?.FirstAncestorOrSelf<VariableDeclaratorSyntax>() is not { } variableDeclarator)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        if (semanticModel.GetDeclaredSymbol(variableDeclarator, context.CancellationToken) is not IFieldSymbol { IsFixedSizeBuffer: true } field)
            return;

        if (field.FixedSize is < 2 or > 16)
            return;

        var inlineArrayType = semanticModel.Compilation.GetBestTypeByMetadataName($"System.Runtime.CompilerServices.InlineArray{field.FixedSize.ToString(CultureInfo.InvariantCulture)}`1");
        if (inlineArrayType is null)
            return;

        var title = $"Use InlineArray{field.FixedSize.ToString(CultureInfo.InvariantCulture)}<T>";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => RefactorAsync(context.Document, variableDeclarator, field.FixedSize, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    private static async Task<Document> RefactorAsync(Document document, VariableDeclaratorSyntax variableDeclarator, int size, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root?.FindNode(variableDeclarator.Span, getInnermostNodeForTie: true).FirstAncestorOrSelf<VariableDeclaratorSyntax>() is not { } currentVariableDeclarator)
            return document;

        if (currentVariableDeclarator.Parent is not VariableDeclarationSyntax variableDeclaration)
            return document;

        if (variableDeclaration.Parent is not FieldDeclarationSyntax fieldDeclaration)
            return document;

        if (variableDeclaration.Variables.Count != 1)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        if (editor.SemanticModel.GetTypeInfo(variableDeclaration.Type, cancellationToken).Type is not { } elementType)
            return document;

        var inlineArrayType = editor.SemanticModel.Compilation.GetBestTypeByMetadataName($"System.Runtime.CompilerServices.InlineArray{size.ToString(CultureInfo.InvariantCulture)}`1");
        if (inlineArrayType is null)
            return document;

        var constructedInlineArrayType = inlineArrayType.Construct(elementType);

        if (generator.TypeExpression(constructedInlineArrayType) is not TypeSyntax generatedType)
            return document;

        var newType = generatedType.WithTriviaFrom(variableDeclaration.Type);

        var newVariable = SyntaxFactory.VariableDeclarator(currentVariableDeclarator.Identifier)
                                       .WithLeadingTrivia(currentVariableDeclarator.GetLeadingTrivia())
                                       .WithTrailingTrivia(currentVariableDeclarator.GetTrailingTrivia());

        var fixedToken = fieldDeclaration.Modifiers.FirstOrDefault(token => token.IsKind(SyntaxKind.FixedKeyword));
        var modifiers = fixedToken.RawKind == 0
            ? fieldDeclaration.Modifiers
            : fieldDeclaration.Modifiers.Remove(fixedToken);

        var newFieldDeclaration = fieldDeclaration.WithModifiers(modifiers)
                                                  .WithDeclaration(variableDeclaration.WithType(newType).WithVariables(SyntaxFactory.SingletonSeparatedList(newVariable)))
                                                  .WithAdditionalAnnotations(Formatter.Annotation);

        editor.ReplaceNode(fieldDeclaration, newFieldDeclaration);
        return editor.GetChangedDocument();
    }
}
