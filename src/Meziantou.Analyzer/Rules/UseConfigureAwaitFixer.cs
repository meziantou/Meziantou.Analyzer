using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseConfigureAwaitFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseConfigureAwaitFalse);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: false);
        if (nodeToFix == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use ConfigureAwait(false)",
                ct => AddConfigureAwait(context.Document, nodeToFix, value: false, ct),
                equivalenceKey: "Use ConfigureAwait(false)"),
            context.Diagnostics);

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use ConfigureAwait(true)",
                ct => AddConfigureAwait(context.Document, nodeToFix, value: true, ct),
                equivalenceKey: "Use ConfigureAwait(true)"),
            context.Diagnostics);
    }

    private static async Task<Document> AddConfigureAwait(Document document, SyntaxNode nodeToFix, bool value, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        // TODO find await using
        if (nodeToFix is AwaitExpressionSyntax awaitSyntax)
        {
            if (awaitSyntax?.Expression != null)
            {
                var newExpression = (ExpressionSyntax)generator.InvocationExpression(
                    generator.MemberAccessExpression(awaitSyntax.Expression, nameof(Task.ConfigureAwait)),
                    generator.LiteralExpression(value));

                var newInvokeExpression = awaitSyntax.WithExpression(newExpression);

                editor.ReplaceNode(nodeToFix, newInvokeExpression);
                return editor.GetChangedDocument();
            }
        }
        else if (nodeToFix is ExpressionSyntax expressionSyntax)
        {
            // await using (var a = expr);
            // var a = expr; await using (a.ConfigureAwait(false));
            var usingBlock = expressionSyntax.Ancestors(ascendOutOfTrivia: true).OfType<UsingStatementSyntax>().FirstOrDefault();
            if (usingBlock != null)
            {
                if (usingBlock.Declaration != null && usingBlock.Declaration.Variables.Count == 1)
                {
                    // Move statement before using
                    // foreach variable, add
                    var variablesStatement = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(usingBlock.Declaration.Type, usingBlock.Declaration.Variables))
                        .WithLeadingTrivia(usingBlock.GetLeadingTrivia());
                    var newUsingBlock = usingBlock
                        .WithDeclaration(null)
                        .WithExpression(AppendConfigureAwait(SyntaxFactory.IdentifierName(usingBlock.Declaration.Variables[0].Identifier)))
                        .WithoutLeadingTrivia();

                    editor.ReplaceNode(usingBlock, newUsingBlock);
                    editor.InsertBefore(newUsingBlock, variablesStatement);
                    return editor.GetChangedDocument();
                }
            }
            else
            {
                // await using var a = expr;
                // var a = expr; await var aConfigured = a.ConfigureAwait(false);
                var usingStatement = expressionSyntax.Ancestors(ascendOutOfTrivia: true).OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                if (usingStatement != null && usingStatement.Declaration.Variables.Count == 1)
                {
                    var variablesStatement = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(usingStatement.Declaration.Type, usingStatement.Declaration.Variables))
                            .WithLeadingTrivia(usingStatement.GetLeadingTrivia());


                    var usingStatements = SyntaxFactory.Block();
                    var statements = usingStatement.Parent as BlockSyntax;
                    if (statements != null)
                    {
                        var index = statements.Statements.IndexOf(usingStatement);
                        usingStatements = SyntaxFactory.Block(SyntaxFactory.List(statements.Statements.Skip(index + 1)));

                        foreach (var node in statements.Statements.Skip(index + 1))
                        {
                            editor.RemoveNode(node);
                        }
                    }

                    var newUsingStatement = SyntaxFactory.UsingStatement(
                        declaration: null,
                        expression: AppendConfigureAwait(SyntaxFactory.IdentifierName(usingStatement.Declaration.Variables[0].Identifier)),
                        statement: usingStatements.WithLeadingTrivia(usingStatement.GetTrailingTrivia()))
                            .WithUsingKeyword(usingStatement.UsingKeyword)
                            .WithAwaitKeyword(usingStatement.AwaitKeyword)
                            .WithLeadingTrivia(usingStatement.GetLeadingTrivia());


                    editor.ReplaceNode(usingStatement, newUsingStatement);
                    editor.InsertBefore(newUsingStatement, variablesStatement);
                    return editor.GetChangedDocument();
                }
            }

            editor.ReplaceNode(nodeToFix, AppendConfigureAwait(nodeToFix));
            return editor.GetChangedDocument();
        }

        return document;

        ExpressionSyntax AppendConfigureAwait(SyntaxNode expressionSyntax)
        {
            return (ExpressionSyntax)generator.InvocationExpression(
                generator.MemberAccessExpression(expressionSyntax, nameof(Task.ConfigureAwait)),
                generator.LiteralExpression(value));
        }
    }
}
