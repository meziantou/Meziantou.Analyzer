using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MakeMethodStaticFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MakeMethodStatic, RuleIdentifiers.MakePropertyStatic);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not (MethodDeclarationSyntax or PropertyDeclarationSyntax))
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel?.GetDeclaredSymbol(nodeToFix, context.CancellationToken) is not { } symbol)
            return;

        var title = "Add static modifier";
        var codeAction = CodeAction.Create(
            title,
            ct => AddStaticModifier(context.Document, nodeToFix, symbol, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Solution> AddStaticModifier(Document document, SyntaxNode nodeToFix, ISymbol symbol, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var solutionEditor = new SolutionEditor(solution);
        var editor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);

        // The member can reference itself, so the declaration is updated from its current node, which contains the updated references
        // A static member cannot be readonly (CS8657): readonly void M() => static void M()
        editor.ReplaceNode(nodeToFix, (currentNode, _) => currentNode switch
        {
            MethodDeclarationSyntax method when method.Modifiers.Count == 0 => method.WithReturnType(method.ReturnType.WithoutLeadingTrivia()).WithModifiers(CreateStaticModifier(method.ReturnType)),
            MethodDeclarationSyntax method => method.WithModifiers(AddStaticModifier(method.Modifiers)).WithAdditionalAnnotations(Formatter.Annotation),
            PropertyDeclarationSyntax property when property.Modifiers.Count == 0 => RemoveReadOnlyAccessors(property.WithType(property.Type.WithoutLeadingTrivia()).WithModifiers(CreateStaticModifier(property.Type))),
            PropertyDeclarationSyntax property => RemoveReadOnlyAccessors(property.WithModifiers(AddStaticModifier(property.Modifiers))).WithAdditionalAnnotations(Formatter.Annotation),
            _ => currentNode,
        });

        // A static member cannot be accessed with an instance reference (CS0176): this.Member => Type.Member
        // The analyzer does not report the members that are accessed through the other instances.
        var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
        foreach (var location in references.SelectMany(reference => reference.Locations))
        {
            // Source generated documents cannot be edited
            if (solution.GetDocument(location.Document.Id) is null)
                continue;

            var referenceEditor = await solutionEditor.GetDocumentEditorAsync(location.Document.Id, cancellationToken).ConfigureAwait(false);
            var reference = referenceEditor.OriginalRoot.FindNode(location.Location.SourceSpan, getInnermostNodeForTie: true);
            if (reference.Parent is not MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax } memberAccess || memberAccess.Name != reference)
                continue;

            // The containing type of the referenced member has the type arguments of the call site: base.Member => Base<int>.Member
            if (referenceEditor.SemanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol?.ContainingType is not { } containingType)
                continue;

            var typeExpression = ((TypeSyntax)referenceEditor.Generator.TypeExpression(containingType)).AsExpressionSyntax().WithTriviaFrom(memberAccess.Expression);
            referenceEditor.ReplaceNode(memberAccess, memberAccess.WithExpression(typeExpression).WithAdditionalAnnotations(Simplifier.Annotation));
        }

        return solutionEditor.GetChangedSolution();
    }

    // Without modifier, the indentation is the leading trivia of the type
    private static SyntaxTokenList CreateStaticModifier(TypeSyntax type) => SyntaxFactory.TokenList(SyntaxFactory.Token(type.GetLeadingTrivia(), SyntaxKind.StaticKeyword, SyntaxFactory.TriviaList(SyntaxFactory.Space)));

    private static SyntaxTokenList AddStaticModifier(SyntaxTokenList modifiers)
    {
        var readOnlyIndex = modifiers.IndexOf(SyntaxKind.ReadOnlyKeyword);
        if (readOnlyIndex < 0)
            return modifiers.Add(SyntaxKind.StaticKeyword);

        var readOnlyModifier = modifiers[readOnlyIndex];
        return modifiers.Replace(readOnlyModifier, SyntaxFactory.Token(readOnlyModifier.LeadingTrivia, SyntaxKind.StaticKeyword, readOnlyModifier.TrailingTrivia));
    }

    private static PropertyDeclarationSyntax RemoveReadOnlyAccessors(PropertyDeclarationSyntax property)
    {
        if (property.AccessorList is null)
            return property;

        var accessors = property.AccessorList.Accessors.Select(accessor =>
        {
            var readOnlyIndex = accessor.Modifiers.IndexOf(SyntaxKind.ReadOnlyKeyword);
            if (readOnlyIndex < 0)
                return accessor;

            // The leading trivia of the first modifier is the indentation of the accessor
            var leadingTrivia = accessor.GetLeadingTrivia();
            return accessor.WithModifiers(accessor.Modifiers.RemoveAt(readOnlyIndex)).WithoutLeadingTrivia().WithLeadingTrivia(leadingTrivia);
        });

        return property.WithAccessorList(property.AccessorList.WithAccessors(SyntaxFactory.List(accessors)));
    }
}
