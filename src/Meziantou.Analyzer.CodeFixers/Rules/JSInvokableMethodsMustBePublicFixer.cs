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
public sealed class JSInvokableMethodsMustBePublicFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.JSInvokableMethodsMustBePublic);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        const string Title = "Make method public";
        foreach (var diagnostic in context.Diagnostics)
        {
            var methodDeclaration = GetMethodDeclaration(root, semanticModel, diagnostic, context.CancellationToken);
            if (methodDeclaration is null || methodDeclaration.ExplicitInterfaceSpecifier is not null)
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => MakeMethodPublic(context.Document, methodDeclaration, ct),
                    equivalenceKey: Title),
                diagnostic);
        }
    }

    private static async Task<Document> MakeMethodPublic(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var modifiers = default(SyntaxTokenList);
        var hasAccessibilityModifier = false;
        foreach (var modifier in methodDeclaration.Modifiers)
        {
            if (IsAccessibilityModifier(modifier))
            {
                if (!hasAccessibilityModifier)
                {
                    modifiers = modifiers.Add(SyntaxFactory.Token(modifier.LeadingTrivia, SyntaxKind.PublicKeyword, modifier.TrailingTrivia));
                    hasAccessibilityModifier = true;
                }
            }
            else
            {
                modifiers = modifiers.Add(modifier);
            }
        }

        if (!hasAccessibilityModifier)
        {
            modifiers = modifiers.Insert(0, SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }

        editor.ReplaceNode(methodDeclaration, methodDeclaration.WithModifiers(modifiers));
        return editor.GetChangedDocument();
    }

    private static bool IsAccessibilityModifier(SyntaxToken modifier)
    {
        return modifier.IsKind(SyntaxKind.PrivateKeyword) ||
               modifier.IsKind(SyntaxKind.InternalKeyword) ||
               modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
               modifier.IsKind(SyntaxKind.PublicKeyword);
    }

    private static MethodDeclarationSyntax? GetMethodDeclaration(SyntaxNode? root, SemanticModel? semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        if (semanticModel?.GetEnclosingSymbol(diagnostic.Location.SourceSpan.Start, cancellationToken) is IMethodSymbol methodSymbol)
        {
            return methodSymbol.DeclaringSyntaxReferences
                .Select(reference => reference.GetSyntax(cancellationToken))
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault();
        }

        var nodeToFix = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        return nodeToFix?.FirstAncestorOrSelf<MethodDeclarationSyntax>()
            ?? root?.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.FirstAncestorOrSelf<MethodDeclarationSyntax>()
            ?? root?.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(method => method.Identifier.Span.IntersectsWith(diagnostic.Location.SourceSpan) || method.Span.IntersectsWith(diagnostic.Location.SourceSpan));
    }
}
