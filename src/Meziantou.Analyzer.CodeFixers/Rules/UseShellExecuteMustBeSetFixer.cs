using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseShellExecuteMustBeSetFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseShellExecuteMustBeSet);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        var objectCreation = nodeToFix?.FirstAncestorOrSelf<ObjectCreationExpressionSyntax>();
        if (objectCreation is null)
            return;

        const string SetFalseTitle = "Set UseShellExecute to false";
        context.RegisterCodeFix(
            CodeAction.Create(
                SetFalseTitle,
                ct => SetUseShellExecute(context.Document, objectCreation, false, ct),
                equivalenceKey: SetFalseTitle),
            context.Diagnostics);

        const string SetTrueTitle = "Set UseShellExecute to true";
        context.RegisterCodeFix(
            CodeAction.Create(
                SetTrueTitle,
                ct => SetUseShellExecute(context.Document, objectCreation, true, ct),
                equivalenceKey: SetTrueTitle),
            context.Diagnostics);
    }

    private static async Task<Document> SetUseShellExecute(Document document, ObjectCreationExpressionSyntax nodeToFix, bool value, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var assignment = AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            IdentifierName("UseShellExecute"),
            value ? LiteralExpression(SyntaxKind.TrueLiteralExpression) : LiteralExpression(SyntaxKind.FalseLiteralExpression));

        var updatedNode = nodeToFix;
        if (nodeToFix.Initializer is null)
        {
            updatedNode = updatedNode.WithInitializer(
                InitializerExpression(
                    SyntaxKind.ObjectInitializerExpression,
                    SingletonSeparatedList<ExpressionSyntax>(assignment)));
        }
        else
        {
            updatedNode = updatedNode.WithInitializer(nodeToFix.Initializer.AddExpressions(assignment));
        }

        editor.ReplaceNode(nodeToFix, updatedNode.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}
