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

        // The diagnostic is reported on the type name identifier, so we need to find the type declaration.
        // Enums and delegates are not TypeDeclarationSyntax, so the declaration must be the one whose identifier is reported,
        // otherwise the fix would apply to the containing type.
        var typeDeclarationSyntax = nodeToFix.FirstAncestorOrSelf<MemberDeclarationSyntax>(node => node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax);
        var identifier = typeDeclarationSyntax switch
        {
            BaseTypeDeclarationSyntax declaration => declaration.Identifier,
            DelegateDeclarationSyntax declaration => declaration.Identifier,
            _ => default,
        };

        if (typeDeclarationSyntax is null || identifier.Span != context.Span)
            return;

        // Code fix 1: Add DynamicallyAccessedMembers attribute (the attribute cannot be applied to enums and delegates)
        if (typeDeclarationSyntax is TypeDeclarationSyntax)
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

    private static async Task<Document> AddDynamicallyAccessedMembersAttribute(Document document, MemberDeclarationSyntax typeDeclarationSyntax, INamedTypeSymbol dynamicallyAccessedMembersAttribute, INamedTypeSymbol dynamicallyAccessedMemberTypes, CancellationToken cancellationToken)
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

    private static async Task<Solution> RemoveTypeDeclaration(Document document, MemberDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;

        // All the declarations of a partial type must be removed, as the remaining ones would still declare the type
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var typeSymbol = semanticModel?.GetDeclaredSymbol(typeDeclarationSyntax, cancellationToken);
        var declarationsByDocument = new Dictionary<DocumentId, HashSet<MemberDeclarationSyntax>>();
        if (typeSymbol is not null)
        {
            foreach (var syntaxReference in typeSymbol.DeclaringSyntaxReferences)
            {
                if (solution.GetDocumentId(syntaxReference.SyntaxTree) is not { } documentId)
                    continue;

                if (await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is not MemberDeclarationSyntax declaration)
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

        // The using directives that reference the type (using static, using alias) would not compile once the type is removed.
        // The analyzer does not consider them as usages, so they are removed with the type. They can be in any document (global using).
        var usingDirectivesByDocument = new Dictionary<DocumentId, List<UsingDirectiveSyntax>>();
        if (typeSymbol is not null)
        {
            foreach (var projectDocument in document.Project.Documents)
            {
                var usingDirectives = await GetUsingDirectivesReferencingType(projectDocument, typeSymbol, cancellationToken).ConfigureAwait(false);
                if (usingDirectives.Count > 0)
                {
                    usingDirectivesByDocument.Add(projectDocument.Id, usingDirectives);
                }
            }
        }

        foreach (var (documentId, declarations) in declarationsByDocument)
        {
            usingDirectivesByDocument.TryGetValue(documentId, out var usingDirectives);
            solution = await RemoveNodes(solution, documentId, declarations, usingDirectives, cancellationToken).ConfigureAwait(false);
        }

        foreach (var (documentId, usingDirectives) in usingDirectivesByDocument)
        {
            if (declarationsByDocument.ContainsKey(documentId))
                continue;

            solution = await RemoveNodes(solution, documentId, declarations: [], usingDirectives, cancellationToken).ConfigureAwait(false);
        }

        return solution;
    }

    private static async Task<List<UsingDirectiveSyntax>> GetUsingDirectivesReferencingType(Document document, ISymbol typeSymbol, CancellationToken cancellationToken)
    {
        var result = new List<UsingDirectiveSyntax>();
        if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not CompilationUnitSyntax compilationUnit)
            return result;

        SemanticModel? semanticModel = null;
        foreach (var usingDirective in GetUsingDirectives(compilationUnit))
        {
            foreach (var name in usingDirective.DescendantNodes().OfType<SimpleNameSyntax>())
            {
                // Only bind the names that can reference the type
                if (name.Identifier.ValueText != typeSymbol.Name)
                    continue;

                semanticModel ??= await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (semanticModel is null)
                    return result;

                if (semanticModel.GetSymbolInfo(name, cancellationToken).Symbol is INamedTypeSymbol referencedType && SymbolEqualityComparer.Default.Equals(referencedType.OriginalDefinition, typeSymbol))
                {
                    result.Add(usingDirective);
                    break;
                }
            }
        }

        return result;
    }

    private static IEnumerable<UsingDirectiveSyntax> GetUsingDirectives(SyntaxNode node)
    {
        var (usings, members) = node switch
        {
            CompilationUnitSyntax compilationUnit => (compilationUnit.Usings, compilationUnit.Members),
            BaseNamespaceDeclarationSyntax namespaceDeclaration => (namespaceDeclaration.Usings, namespaceDeclaration.Members),
            _ => default,
        };

        foreach (var usingDirective in usings)
        {
            yield return usingDirective;
        }

        foreach (var member in members)
        {
            if (member is BaseNamespaceDeclarationSyntax)
            {
                foreach (var usingDirective in GetUsingDirectives(member))
                {
                    yield return usingDirective;
                }
            }
        }
    }

    private static async Task<Solution> RemoveNodes(Solution solution, DocumentId documentId, HashSet<MemberDeclarationSyntax> declarations, List<UsingDirectiveSyntax>? usingDirectives, CancellationToken cancellationToken)
    {
        var document = solution.GetDocument(documentId);
        if (document is null)
            return solution;

        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return solution;

        // If the file only contains the removed declarations and there are no assembly attributes, remove the document from the solution
        if (declarations.Count > 0 && root is CompilationUnitSyntax compilationUnit && compilationUnit.AttributeLists.Count == 0 && GetAllTypeDeclarations(compilationUnit).All(declarations.Contains))
            return solution.RemoveDocument(documentId);

        // Otherwise, just remove the type declarations and the using directives that reference the type
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        foreach (var declaration in declarations)
        {
            editor.RemoveNode(declaration);
        }

        if (usingDirectives is not null)
        {
            foreach (var usingDirective in usingDirectives)
            {
                editor.RemoveNode(usingDirective);
            }
        }

        return editor.GetChangedDocument().Project.Solution;
    }

    private static List<MemberDeclarationSyntax> GetAllTypeDeclarations(SyntaxNode node)
    {
        var types = new List<MemberDeclarationSyntax>();
        CollectTypeDeclarations(node, types);
        return types;
    }

    private static void CollectTypeDeclarations(SyntaxNode node, List<MemberDeclarationSyntax> types)
    {
        // The nested types are removed with their containing type, so only the outermost type declarations are collected
        if (node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
        {
            types.Add((MemberDeclarationSyntax)node);
            return;
        }

        foreach (var child in node.ChildNodes())
        {
            CollectTypeDeclarations(child, types);
        }
    }
}
