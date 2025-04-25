using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Analyzer.Internals;
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
        if (nodeToFix is null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use ConfigureAwait(false)",
                ct => AddConfigureAwait(context, nodeToFix, value: false, ct),
                equivalenceKey: "Use ConfigureAwait(false)"),
            context.Diagnostics);

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use ConfigureAwait(true)",
                ct => AddConfigureAwait(context, nodeToFix, value: true, ct),
                equivalenceKey: "Use ConfigureAwait(true)"),
            context.Diagnostics);
    }

    private static async Task<Document> AddConfigureAwait(CodeFixContext context, SyntaxNode nodeToFix, bool value, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        if (context.Diagnostics[0].Properties.TryGetValue("kind", out var kind))
        {
            if (kind is "foreach")
            {
                var foreachSyntax = nodeToFix.FirstAncestorOrSelf<ForEachStatementSyntax>();
                if (foreachSyntax is null)
                    return editor.GetChangedDocument();

                var collection = foreachSyntax.Expression;
                var newExpression = (ExpressionSyntax)generator.InvocationExpression(
                       generator.MemberAccessExpression(collection.Parentheses(), nameof(Task.ConfigureAwait)),
                       generator.LiteralExpression(value))
                       .Parentheses();

                var newForeachSyntax = foreachSyntax.WithExpression(newExpression);

                editor.ReplaceNode(foreachSyntax, newForeachSyntax);
                return editor.GetChangedDocument();
            }
            else if (kind is "using")
            {
                var usingSyntax = nodeToFix.FirstAncestorOrSelf<UsingStatementSyntax>();
                if (usingSyntax is null)
                    return editor.GetChangedDocument();

                var expression = usingSyntax.Expression;
                if (expression is not null)
                {
                    var newExpression = (ExpressionSyntax)generator.InvocationExpression(
                           generator.MemberAccessExpression(expression.Parentheses(), nameof(Task.ConfigureAwait)),
                           generator.LiteralExpression(value))
                           .Parentheses();

                    var newSyntax = usingSyntax.WithExpression(newExpression);
                    editor.ReplaceNode(usingSyntax, newSyntax);
                }

                return editor.GetChangedDocument();
            }
        }

        if (nodeToFix is AwaitExpressionSyntax awaitSyntax)
        {
            if (awaitSyntax.Expression is not null)
            {
                var newExpression = (ExpressionSyntax)generator.InvocationExpression(
                    generator.MemberAccessExpression(awaitSyntax.Expression.Parentheses(), nameof(Task.ConfigureAwait)),
                    generator.LiteralExpression(value))
                    .Parentheses();

                var newInvokeExpression = awaitSyntax.WithExpression(newExpression);

                editor.ReplaceNode(nodeToFix, newInvokeExpression);
                return editor.GetChangedDocument();
            }
        }
        else if (nodeToFix is VariableDeclaratorSyntax or ExpressionSyntax)
        {
            // await using (var a = expr);
            // var a = expr; await using (a.ConfigureAwait(false));
            var usingBlock = nodeToFix.Ancestors(ascendOutOfTrivia: true).OfType<UsingStatementSyntax>().FirstOrDefault();
            if (usingBlock is not null)
            {
                if (usingBlock.Declaration is not null && usingBlock.Declaration.Variables.Count == 1)
                {
                    // Move statement before using
                    // foreach variable, add
                    var variablesStatement = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(usingBlock.Declaration.Type, usingBlock.Declaration.Variables))
                        .WithLeadingTrivia(usingBlock.GetLeadingTrivia());
                    var newUsingBlock = usingBlock
                        .WithDeclaration(null)
                        .WithExpression(AppendConfigureAwait(SyntaxFactory.IdentifierName(usingBlock.Declaration.Variables[0].Identifier)))
                        .WithoutLeadingTrivia();

                    editor.InsertBefore(usingBlock, variablesStatement);
                    editor.ReplaceNode(usingBlock, newUsingBlock);
                    return editor.GetChangedDocument();
                }
            }
            else
            {
                // await using var a = expr;
                // var a = expr; await var aConfigured = a.ConfigureAwait(false);
                var usingStatement = nodeToFix.Ancestors(ascendOutOfTrivia: true).OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
                if (usingStatement is not null && usingStatement.Declaration.Variables.Count == 1)
                {
                    var variablesStatement = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(usingStatement.Declaration.Type, usingStatement.Declaration.Variables))
                            .WithLeadingTrivia(usingStatement.GetLeadingTrivia());

                    if (usingStatement.Parent is GlobalStatementSyntax { Statement: LocalDeclarationStatementSyntax, Parent: CompilationUnitSyntax compilationUnit } globalStatement)
                    {
                        var index = compilationUnit.Members.IndexOf(globalStatement);
                        var usingStatements = SyntaxFactory.Block(SyntaxFactory.List(compilationUnit.Members
                            .Skip(index + 1)
                            .TakeWhile(m => m.IsKind(SyntaxKind.GlobalStatement))
                            .Select(m => ((GlobalStatementSyntax)m).Statement)));

                        foreach (var node in compilationUnit.Members.Skip(index + 1).TakeWhile(m => m.IsKind(SyntaxKind.GlobalStatement)))
                        {
                            editor.RemoveNode(node);
                        }

                        var newUsingStatement = SyntaxFactory.UsingStatement(
                            declaration: null,
                            expression: AppendConfigureAwait(SyntaxFactory.IdentifierName(usingStatement.Declaration.Variables[0].Identifier)),
                            statement: usingStatements)
                                .WithUsingKeyword(usingStatement.UsingKeyword)
                                .WithAwaitKeyword(usingStatement.AwaitKeyword)
                                .WithLeadingTrivia(usingStatement.GetLeadingTrivia());

                        editor.InsertBefore(usingStatement, variablesStatement);
                        editor.ReplaceNode(usingStatement, newUsingStatement);
                    }
                    else
                    {
                        var usingStatements = SyntaxFactory.Block();
                        if (usingStatement.Parent is BlockSyntax statements)
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

                        editor.InsertBefore(usingStatement, variablesStatement);
                        editor.ReplaceNode(usingStatement, newUsingStatement);
                    }

                    return editor.GetChangedDocument();
                }
            }

            editor.ReplaceNode(nodeToFix, AppendConfigureAwait(nodeToFix));
            return editor.GetChangedDocument();
        }

        return context.Document;

        ExpressionSyntax AppendConfigureAwait(SyntaxNode expressionSyntax)
        {
            return (ExpressionSyntax)generator.InvocationExpression(
                generator.MemberAccessExpression(expressionSyntax, nameof(Task.ConfigureAwait)),
                generator.LiteralExpression(value)).Parentheses();
        }
    }
}
