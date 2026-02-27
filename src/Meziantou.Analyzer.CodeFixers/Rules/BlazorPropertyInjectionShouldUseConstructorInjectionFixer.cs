#if CSHARP12_OR_GREATER
using System.Collections.Immutable;
using System.Composition;
using System.Text;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class BlazorPropertyInjectionShouldUseConstructorInjectionFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleIdentifiers.BlazorPropertyInjectionShouldUseConstructorInjection);

    public override FixAllProvider GetFixAllProvider() =>
        BlazorPropertyInjectionFixAllProvider.Instance;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        var title = "Use constructor injection";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                ct => FixDocumentAsync(context.Document, context.Diagnostics, ct),
                equivalenceKey: title),
            context.Diagnostics);
    }

    internal static async Task<Solution> FixDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || semanticModel is null)
            return solution;

        // Collect all (property symbol, parameter name) pairs from the diagnostics
        var propertiesToFix = new List<(IPropertySymbol Symbol, string ParameterName, TypeSyntax PropertyType)>();
        foreach (var diagnostic in diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var propertyDecl = node?.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (propertyDecl is null)
                continue;

            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDecl, cancellationToken) as IPropertySymbol;
            if (propertySymbol is null)
                continue;

            var classDecl = propertyDecl.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (classDecl is null || HasExplicitNonPrimaryConstructors(classDecl))
                continue;

            var parameterName = ComputeParameterName(propertySymbol.Name);
            propertiesToFix.Add((propertySymbol, parameterName, propertyDecl.Type.WithoutTrivia()));
        }

        if (propertiesToFix.Count == 0)
            return solution;

        // Group by containing class (to handle multiple properties in the same class)
        var byClass = propertiesToFix.GroupBy(p => p.Symbol.ContainingType, SymbolEqualityComparer.Default).ToList();

        foreach (var classGroup in byClass)
        {
            var properties = classGroup.ToList();
            var firstClassDecl = await GetClassDeclarationAsync(document, solution, properties[0].Symbol, cancellationToken).ConfigureAwait(false);
            if (firstClassDecl is null)
                continue;

            // Annotate the class so we can find it after all renames
            document = solution.GetDocument(document.Id)!;
            root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                continue;

            var classAnnotation = new SyntaxAnnotation();
            var classDeclNode = root.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(t => t.Identifier.ValueText == firstClassDecl.Identifier.ValueText);
            if (classDeclNode is null)
                continue;

            root = root.ReplaceNode(classDeclNode, classDeclNode.WithAdditionalAnnotations(classAnnotation));
            document = document.WithSyntaxRoot(root);
            solution = document.Project.Solution;

            // Rename each property sequentially; find each by its current identifier
            // After each rename, the property name changes but the type stays the same
            var parameterNames = properties.Select(p => p.ParameterName).ToHashSet(StringComparer.Ordinal);

            foreach (var (propSymbol, paramName, _) in properties)
            {
                // Get fresh state for each rename
                document = solution.GetDocument(document.Id)!;
                root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (root is null || semanticModel is null)
                    continue;

                // Find the class by annotation
                var currentClassDecl = root.GetAnnotatedNodes(classAnnotation).OfType<TypeDeclarationSyntax>().FirstOrDefault();
                if (currentClassDecl is null)
                    continue;

                // Find the property by original name (propSymbol.Name is the original name)
                var currentPropDecl = currentClassDecl.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(p => p.Identifier.ValueText == propSymbol.Name);
                if (currentPropDecl is null)
                    continue;

                var currentPropSymbol = semanticModel.GetDeclaredSymbol(currentPropDecl, cancellationToken) as IPropertySymbol;
                if (currentPropSymbol is null)
                    continue;

                // Rename using Renamer
                solution = await Renamer.RenameSymbolAsync(solution, currentPropSymbol, new SymbolRenameOptions(), paramName, cancellationToken).ConfigureAwait(false);
            }

            // After all renames, apply structural changes to the class
            document = solution.GetDocument(document.Id)!;
            root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                continue;

            var updatedClassDecl = root.GetAnnotatedNodes(classAnnotation).OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (updatedClassDecl is null)
                continue;

            // Find all renamed [Inject] properties (now with camelCase identifiers)
            var renamedProperties = updatedClassDecl.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => parameterNames.Contains(p.Identifier.ValueText))
                .ToList();

            // Build parameter list from original type info (ordered same as diagnostics)
            var newParams = properties
                .Select(p => Parameter(
                    List<AttributeListSyntax>(),
                    TokenList(),
                    p.PropertyType,
                    Identifier(p.ParameterName),
                    null))
                .ToList();

            TypeDeclarationSyntax newClassDecl;
            if (updatedClassDecl.ParameterList is not null)
            {
                var existingParams = updatedClassDecl.ParameterList.Parameters;
                ParameterListSyntax newParamList;
                if (existingParams.Count > 0)
                {
                    var paramsWithSeparator = newParams.Select(p => p.WithLeadingTrivia(Space));
                    newParamList = updatedClassDecl.ParameterList.AddParameters([.. paramsWithSeparator]);
                }
                else
                {
                    newParamList = updatedClassDecl.ParameterList.WithParameters(
                        SeparatedList(newParams, Enumerable.Repeat(Token(SyntaxKind.CommaToken).WithTrailingTrivia(Space), newParams.Count - 1)));
                }

                newClassDecl = updatedClassDecl.WithParameterList(newParamList);
            }
            else
            {
                var newParamList = ParameterList(
                    SeparatedList(newParams, Enumerable.Repeat(Token(SyntaxKind.CommaToken).WithTrailingTrivia(Space), newParams.Count - 1)));
                newClassDecl = updatedClassDecl.WithParameterList(newParamList);
            }

            // Remove all renamed [Inject] properties
            foreach (var renamedProp in renamedProperties)
            {
                var propToRemove = newClassDecl.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .FirstOrDefault(p => p.Identifier.ValueText == renamedProp.Identifier.ValueText);
                if (propToRemove is not null)
                {
                    newClassDecl = newClassDecl.RemoveNode(propToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;
                }
            }

            root = root.ReplaceNode(updatedClassDecl, newClassDecl);
            document = document.WithSyntaxRoot(root);
            solution = document.Project.Solution;
        }

        return solution;
    }

    private static async Task<TypeDeclarationSyntax?> GetClassDeclarationAsync(Document document, Solution solution, IPropertySymbol propertySymbol, CancellationToken cancellationToken)
    {
        var doc = solution.GetDocument(document.Id)!;
        var root = await doc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return null;

        var semanticModel = await doc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return null;

        return root.DescendantNodes().OfType<TypeDeclarationSyntax>()
            .FirstOrDefault(t =>
            {
                var symbol = semanticModel.GetDeclaredSymbol(t, cancellationToken);
                return SymbolEqualityComparer.Default.Equals(symbol, propertySymbol.ContainingType);
            });
    }

    private static bool HasExplicitNonPrimaryConstructors(TypeDeclarationSyntax typeDeclaration)
    {
        return typeDeclaration.Members.OfType<ConstructorDeclarationSyntax>().Any();
    }

    internal static string ComputeParameterName(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return propertyName;

        var sb = new StringBuilder(propertyName);
        sb[0] = char.ToLowerInvariant(sb[0]);
        return sb.ToString();
    }
}
#endif
