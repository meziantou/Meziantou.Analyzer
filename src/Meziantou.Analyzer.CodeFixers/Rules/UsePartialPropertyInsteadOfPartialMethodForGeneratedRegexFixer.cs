using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UsePartialPropertyInsteadOfPartialMethodForGeneratedRegexFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(RuleIdentifiers.UsePartialPropertyInsteadOfPartialMethodForGeneratedRegex);

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        var methodDeclaration = nodeToFix?.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null)
            return;

        const string Title = "Use partial property instead of partial method";
        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                ct => FixAsync(context.Document, context.Diagnostics[0], ct),
                equivalenceKey: Title),
            context.Diagnostics);
    }

    private static async Task<Solution> FixAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document.Project.Solution;

        var nodeToFix = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        var methodDeclaration = nodeToFix?.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (methodDeclaration is null)
            return document.Project.Solution;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return document.Project.Solution;

        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken);
        if (methodSymbol is null)
            return document.Project.Solution;

        var solution = document.Project.Solution;

        // Find all references to the method (call sites) across the solution
        var references = await SymbolFinder.FindReferencesAsync(methodSymbol, solution, cancellationToken).ConfigureAwait(false);
        var referencesByDocument = references
            .SelectMany(r => r.Locations)
            .Where(loc => loc.Location.IsInSource)
            .GroupBy(loc => loc.Document.Id);

        // Replace all invocations (MethodName()) with property accesses (MethodName)
        foreach (var group in referencesByDocument)
        {
            var doc = solution.GetDocument(group.Key);
            if (doc is null)
                continue;

            var docRoot = await doc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (docRoot is null)
                continue;

            var nodesToReplace = new Dictionary<SyntaxNode, SyntaxNode>();
            foreach (var refLoc in group)
            {
                var refNode = docRoot.FindNode(refLoc.Location.SourceSpan, getInnermostNodeForTie: true);
                if (refNode is null)
                    continue;

                var invocation = GetContainingInvocationExpression(refNode);
                if (invocation is not null && invocation.ArgumentList.Arguments.Count == 0)
                {
                    nodesToReplace[invocation] = invocation.Expression.WithTriviaFrom(invocation);
                }
            }

            if (nodesToReplace.Count > 0)
            {
                var newDocRoot = docRoot.ReplaceNodes(nodesToReplace.Keys, (original, _) => nodesToReplace[original]);
                solution = solution.WithDocumentSyntaxRoot(group.Key, newDocRoot);
            }
        }

        // Replace the method declaration with a property declaration
        var updatedDoc = solution.GetDocument(document.Id);
        if (updatedDoc is null)
            return solution;

        var updatedRoot = await updatedDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (updatedRoot is null)
            return solution;

        var currentMethodDecl = updatedRoot
            .FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
            ?.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (currentMethodDecl is null)
            return solution;

        var propertyDeclaration = CreatePropertyDeclaration(currentMethodDecl);
        var newRoot = updatedRoot.ReplaceNode(currentMethodDecl, propertyDeclaration);
        return solution.WithDocumentSyntaxRoot(document.Id, newRoot);
    }

    private static InvocationExpressionSyntax? GetContainingInvocationExpression(SyntaxNode refNode)
    {
        if (refNode.Parent is InvocationExpressionSyntax invocation)
            return invocation;

        if (refNode.Parent is MemberAccessExpressionSyntax && refNode.Parent.Parent is InvocationExpressionSyntax memberInvocation)
            return memberInvocation;

        return null;
    }

    private static PropertyDeclarationSyntax CreatePropertyDeclaration(MethodDeclarationSyntax method)
    {
        var getAccessor = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        var accessorList = AccessorList(SingletonList(getAccessor))
            .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(Space))
            .WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(Space));

        return PropertyDeclaration(method.ReturnType, method.Identifier.WithTrailingTrivia(Space))
            .WithAttributeLists(method.AttributeLists)
            .WithModifiers(method.Modifiers)
            .WithAccessorList(accessorList)
            .WithLeadingTrivia(method.GetLeadingTrivia())
            .WithAdditionalAnnotations(Formatter.Annotation);
    }
}
