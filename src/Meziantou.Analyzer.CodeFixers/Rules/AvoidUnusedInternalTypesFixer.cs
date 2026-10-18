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
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var dynamicallyAccessedMembersAttribute = semanticModel?.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute");
            var dynamicallyAccessedMemberTypes = semanticModel?.Compilation.GetBestTypeByMetadataName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes");
            if (dynamicallyAccessedMembersAttribute is not null && dynamicallyAccessedMemberTypes is not null)
            {
                var title = "Add DynamicallyAccessedMembers attribute";
                var codeAction = CodeAction.Create(
                    title,
                    ct => AddDynamicallyAccessedMembersAttribute(context.Document, typeDeclarationSyntax, dynamicallyAccessedMembersAttribute, dynamicallyAccessedMemberTypes, ct),
                    equivalenceKey: title);

                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
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

    private static async Task<Document> AddDynamicallyAccessedMembersAttribute(Document document, TypeDeclarationSyntax typeDeclarationSyntax, INamedTypeSymbol dynamicallyAccessedMembersAttribute, INamedTypeSymbol dynamicallyAccessedMemberTypes, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        var attribute = generator.Attribute(
            generator.TypeExpression(dynamicallyAccessedMembersAttribute, addImport: true),
            [
                generator.AttributeArgument(
                    generator.TypeMemberAccessExpression(
                        dynamicallyAccessedMemberTypes,
                        nameof(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All),
                        addImport: true)),
            ]);

        editor.AddAttribute(typeDeclarationSyntax, attribute);
        return editor.GetChangedDocument();
    }

    private static async Task<Solution> RemoveTypeDeclaration(Document document, TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;

        // All the declarations of a partial type must be removed, as the remaining ones would still declare the type
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var typeSymbol = semanticModel?.GetDeclaredSymbol(typeDeclarationSyntax, cancellationToken);
        var declarationsByDocument = new Dictionary<DocumentId, HashSet<TypeDeclarationSyntax>>();
        if (typeSymbol is not null)
        {
            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                if (solution.GetDocumentId(syntaxReference.SyntaxTree) is not { } documentId)
                    continue;

                if (await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is not TypeDeclarationSyntax declaration)
                    continue;

                if (!declarationsByDocument.TryGetValue(documentId, out var declarations))
                {
                    declarations = [];
                    declarationsByDocument.Add(documentId, declarations);
                }

                declarations.Add(declaration);
            }
        }

        if (declarationsByDocument.Count == 0)
        {
            declarationsByDocument.Add(document.Id, [typeDeclarationSyntax]);
        }

        foreach (var (documentId, declarations) in declarationsByDocument)
        {
            solution = await RemoveTypeDeclarations(solution, documentId, declarations, cancellationToken).ConfigureAwait(false);
        }

        return solution;
    }

    private static async Task<Solution> RemoveTypeDeclarations(Solution solution, DocumentId documentId, HashSet<TypeDeclarationSyntax> declarations, CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(documentId);
        if (document is null)
            return solution;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return solution;

        // If the file only contains the removed declarations and there are no assembly attributes, remove the document from the solution
        if (root is CompilationUnitSyntax compilationUnit && compilationUnit.AttributeLists.Count == 0 && GetAllTypeDeclarations(compilationUnit).All(declarations.Contains))
            return solution.RemoveDocument(documentId);

        // Otherwise, just remove the type declarations
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        foreach (var declaration in declarations)
        {
            editor.RemoveNode(declaration);
        }

        return editor.GetChangedDocument().Project.Solution;
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
