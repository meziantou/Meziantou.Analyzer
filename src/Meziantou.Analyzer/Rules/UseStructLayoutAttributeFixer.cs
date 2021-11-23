using System.Collections.Immutable;
using System.Composition;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStructLayoutAttributeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MissingStructLayoutAttribute);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Add Auto StructLayout attribute",
                ct => Refactor(context.Document, nodeToFix, LayoutKind.Auto, ct),
                equivalenceKey: "Add Auto StructLayout attribute"),
            context.Diagnostics);

        context.RegisterCodeFix(
            CodeAction.Create(
                "Add Sequential StructLayout attribute",
                ct => Refactor(context.Document, nodeToFix, LayoutKind.Explicit, ct),
                equivalenceKey: "Add Sequential StructLayout attribute"),
            context.Diagnostics);
    }

    private static async Task<Document> Refactor(Document document, SyntaxNode nodeToFix, LayoutKind layoutKind, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var semanticModel = editor.SemanticModel;

        var structLayoutAttribute = semanticModel.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.StructLayoutAttribute");
        var layoutKindEnum = semanticModel.Compilation.GetTypeByMetadataName("System.Runtime.InteropServices.LayoutKind");

        var attribute = editor.Generator.Attribute(
            generator.TypeExpression(structLayoutAttribute, addImport: true),
            new[]
            {
                    generator.AttributeArgument(
                        generator.MemberAccessExpression(
                            generator.TypeExpression(layoutKindEnum, addImport: true),
                            layoutKind.ToString())),
            });

        var structNode = (StructDeclarationSyntax)nodeToFix;

        editor.AddAttribute(structNode, attribute);
        return editor.GetChangedDocument();
    }
}
