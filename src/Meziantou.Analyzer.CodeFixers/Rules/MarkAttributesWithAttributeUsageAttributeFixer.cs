using System.Collections.Immutable;
using System.Composition;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

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
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var attributeUsageAttribute = semanticModel.Compilation.GetBestTypeByMetadataName("System.AttributeUsageAttribute");
        if (attributeUsageAttribute is null)
            return;

        var attributeTargets = semanticModel.Compilation.GetBestTypeByMetadataName("System.AttributeTargets");
        if (attributeTargets is null)
            return;

        var title = "Add AttributeUsage attribute";
        var codeAction = CodeAction.Create(
            title,
            ct => Refactor(context.Document, nodeToFix, attributeUsageAttribute, attributeTargets, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Refactor(Document document, SyntaxNode nodeToFix, INamedTypeSymbol attributeUsageAttribute, INamedTypeSymbol attributeTargets, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var classNode = (ClassDeclarationSyntax)nodeToFix;

        var attribute = editor.Generator.Attribute(
            generator.TypeExpression(attributeUsageAttribute, addImport: true),
            [
                    generator.AttributeArgument(
                        generator.MemberAccessExpression(
                            generator.TypeExpression(attributeTargets, addImport: true),
                            nameof(AttributeTargets.All))),
            ]);

        editor.AddAttribute(classNode, attribute);
        return editor.GetChangedDocument();
    }
}
