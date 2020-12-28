using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class StringShouldNotContainsNonDeterministicEndOfLineFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.StringShouldNotContainsNonDeterministicEndOfLine);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            foreach (var newLine in new[] { null, "\n", "\r\n" })
            {
                var title = "Use explicit " + (newLine switch
                {
                    null => "new lines",
                    _ => $"new lines ({newLine.Replace("\r", "\\r").Replace("\n", "\\n")})",
                });
                var codeAction = CodeAction.Create(
                    title,
                    ct => FixString(context.Document, nodeToFix, newLine, ct),
                    equivalenceKey: title);

                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }

        private static async Task<Document> FixString(Document document, SyntaxNode nodeToFix, string? newLine, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var options = await document.GetOptionsAsync(cancellationToken);
            var lineEnding = options.GetOption(FormattingOptions.NewLine, LanguageNames.CSharp);

            var firstCharPosition = nodeToFix.GetLineSpan()?.StartLinePosition.Character ?? 0;
            var newLineTrivia = TriviaList(lineEnding == "\n" ? LineFeed : CarriageReturnLineFeed, Whitespace(new string(' ', firstCharPosition)));

            if (nodeToFix is LiteralExpressionSyntax literal)
            {
                var text = literal.GetText().ToString();
                var isVerbatim = text.StartsWith("@", StringComparison.Ordinal);
                text = isVerbatim ? text.Substring(2, text.Length - 3) : text.Substring(1, text.Length - 2);

                var newNode = ReplaceString(generator, text, newLine, newLineTrivia);
                editor.ReplaceNode(nodeToFix, newNode);
            }

            return editor.GetChangedDocument();
        }

        private static SyntaxNode ReplaceString(SyntaxGenerator generator, string text, string? newLine, SyntaxTriviaList trivia)
        {
            SyntaxNode? node = null;
            foreach (var (line, eol) in text.SplitLines())
            {
                var newText = eol.Length > 0 ? string.Concat(line.ToString(), newLine ?? eol.ToString()) : line.ToString();
                var literal = generator.LiteralExpression(newText);
                if (node == null)
                {
                    node = literal;
                }
                else
                {
                    literal = literal.WithLeadingTrivia(trivia);
                    node = generator.AddExpression(node, literal);
                }
            }

            return node!;
        }
    }
}
