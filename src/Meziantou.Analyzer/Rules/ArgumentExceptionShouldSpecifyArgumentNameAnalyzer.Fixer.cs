using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class ArgumentExceptionShouldSpecifyArgumentNameFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseNameofOperator);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var diagnostic = context.Diagnostics.First();
            if (!diagnostic.Properties.TryGetValue(ArgumentExceptionShouldSpecifyArgumentNameAnalyzer.ArgumentNameKey, out var argumentName))
                return;

            var title = "Use nameof";
            var codeAction = CodeAction.Create(
                title,
                ct => UseNameof(context.Document, nodeToFix, argumentName, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> UseNameof(Document document, SyntaxNode nodeToFix, string argumentName, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            editor.ReplaceNode(nodeToFix, generator.NameOfExpression(generator.IdentifierName(argumentName)));
            return editor.GetChangedDocument();
        }
    }
}
