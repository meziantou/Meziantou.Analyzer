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
public sealed class AvoidUnusedInternalTypesFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AvoidUnusedInternalTypes);

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

        // The diagnostic is reported on the type name identifier, so we need to find the type declaration
        var typeDeclarationSyntax = nodeToFix.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (typeDeclarationSyntax is null)
            return;

        // Code fix 1: Add DynamicallyAccessedMembers attribute
        {
            var title = "Add DynamicallyAccessedMembers attribute";
            var codeAction = CodeAction.Create(
                title,
                ct => AddDynamicallyAccessedMembersAttribute(context.Document, typeDeclarationSyntax, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        // Code fix 2: Remove the type
        {
            var title = "Remove unused type";
            var codeAction = CodeAction.Create(
                title,
                ct => RemoveTypeDeclaration(context.Document, typeDeclarationSyntax, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> AddDynamicallyAccessedMembersAttribute(Document document, TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        var dynamicallyAccessedMembersAttribute = semanticModel.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute");
        var dynamicallyAccessedMemberTypes = semanticModel.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes");

        if (dynamicallyAccessedMembersAttribute is null || dynamicallyAccessedMemberTypes is null)
            return document;

        var attribute = generator.Attribute(
            generator.TypeExpression(dynamicallyAccessedMembersAttribute, addImport: true),
            [
                generator.AttributeArgument(
                    generator.MemberAccessExpression(
                        generator.TypeExpression(dynamicallyAccessedMemberTypes, addImport: true),
                        nameof(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All))),
            ]);

        editor.AddAttribute(typeDeclarationSyntax, attribute);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> RemoveTypeDeclaration(Document document, TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        // Check if the type is the only type in the file
        var compilationUnit = root as CompilationUnitSyntax;
        if (compilationUnit is not null)
        {
            var allTypes = GetAllTypeDeclarations(compilationUnit);
            
            // If this is the only type in the file, return a document that deletes the file
            if (allTypes.Count == 1 && allTypes.Contains(typeDeclarationSyntax))
            {
                // Return an empty document to signal file deletion
                var newRoot = compilationUnit
                    .WithMembers(default)
                    .WithUsings(default)
                    .WithExterns(default)
                    .WithAttributeLists(default);
                
                return document.WithSyntaxRoot(newRoot);
            }
        }

        // Otherwise, just remove the type declaration
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.RemoveNode(typeDeclarationSyntax);
        return editor.GetChangedDocument();
    }

    private static List<TypeDeclarationSyntax> GetAllTypeDeclarations(SyntaxNode node)
    {
        var types = new List<TypeDeclarationSyntax>();
        CollectTypeDeclarations(node, types);
        return types;
    }

    private static void CollectTypeDeclarations(SyntaxNode node, List<TypeDeclarationSyntax> types)
    {
        if (node is TypeDeclarationSyntax typeDeclaration)
        {
            types.Add(typeDeclaration);
        }

        foreach (var child in node.ChildNodes())
        {
            CollectTypeDeclarations(child, types);
        }
    }
}
