using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ILoggerParameterTypeShouldMatchContainingTypeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ILoggerParameterTypeShouldMatchContainingType);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var title = "Use ILogger with matching type parameter";
        var codeAction = CodeAction.Create(
            title,
            ct => FixAsync(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> FixAsync(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;

        // Find the parameter that contains the node
        var parameter = nodeToFix.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
        if (parameter is null)
            return document;

        // Get the ILogger<T> type
        var parameterType = parameter.Type;
        if (parameterType is not GenericNameSyntax genericType)
            return document;

        // Find the containing type by traversing up the syntax tree
        var containingTypeDeclaration = parameter.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (containingTypeDeclaration is null)
            return document;

        // Get the symbol for the containing type
        var containingTypeSymbol = semanticModel.GetDeclaredSymbol(containingTypeDeclaration, cancellationToken);
        if (containingTypeSymbol is null)
            return document;

        // Create new type argument
        var generator = editor.Generator;
        var newTypeArgument = generator.TypeExpression(containingTypeSymbol);

        // Create new generic type
        var newGenericType = genericType.WithTypeArgumentList(
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList((TypeSyntax)newTypeArgument)));

        editor.ReplaceNode(genericType, newGenericType);
        return editor.GetChangedDocument();
    }
}
