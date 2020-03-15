using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class AvoidUsingRedundantElseFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AvoidUsingRedundantElse);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Remove redundant else";
            var codeAction = CodeAction.Create(
                title,
                ct => RemoveRedundantElse(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> RemoveRedundantElse(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var elseClause = (ElseClauseSyntax)nodeToFix;
            if (elseClause == null)
                return document;

            var ifStatement = elseClause.Parent as IfStatementSyntax;
            if (ifStatement == null)
                return document;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            // Remove else
            root = root.ReplaceNode(ifStatement, ifStatement.WithElse(@else: null));
            ifStatement = (IfStatementSyntax)root.FindNode(ifStatement.IfKeyword.Span);

            // Insert else statements after the if statement
            if (elseClause.Statement != null)
            {
                IEnumerable<StatementSyntax> newStatements;
                if (elseClause.Statement is BlockSyntax blockSyntax)
                {
                    newStatements = blockSyntax.Statements.Select((statement, index) =>
                    {
                        var newStatement = statement;
                        if (index == 0)
                        {
                            newStatement = AddNewLineBefore(newStatement);
                        }

                        return newStatement.WithAdditionalAnnotations(Formatter.Annotation);
                    });
                }
                else
                {
                    newStatements = new[] { AddNewLineBefore(elseClause.Statement).WithAdditionalAnnotations(Formatter.Annotation) };
                }

                if (MustWrapInBlock(ifStatement))
                {
                    root = root.ReplaceNode(ifStatement, SyntaxFactory.Block(new StatementSyntax[] { ifStatement }.Concat(newStatements)).WithAdditionalAnnotations(Formatter.Annotation));
                }
                else
                {
                    root = root.InsertNodesAfter(ifStatement, newStatements);
                }
            }


            return document.WithSyntaxRoot(root);
        }

        private static StatementSyntax AddNewLineBefore(StatementSyntax syntaxNode)
        {
            return syntaxNode.WithLeadingTrivia(syntaxNode.GetLeadingTrivia().Insert(0, SyntaxFactory.LineFeed));
        }

        private static bool MustWrapInBlock(SyntaxNode syntaxNode)
        {
            if (syntaxNode.Parent == null)
                return true;

            return syntaxNode.Parent switch
            {
                IfStatementSyntax statement => statement.Statement == syntaxNode || statement.Else == syntaxNode,
                WhileStatementSyntax statement => statement.Statement == syntaxNode,
                ForEachStatementSyntax statement => statement.Statement == syntaxNode,
                ForStatementSyntax statement => statement.Statement == syntaxNode,
                DoStatementSyntax statement => statement.Statement == syntaxNode,
                _ => false,
            };
        }
    }
}
