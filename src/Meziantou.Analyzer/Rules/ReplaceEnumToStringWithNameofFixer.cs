using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class ReplaceEnumToStringWithNameofFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ReplaceEnumToStringWithNameof);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Use nameof";
            context.RegisterCodeFix(CodeAction.Create(title, ct => UseNameof(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
        }

        private static async Task<Document> UseNameof(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;
            var operation = (IInvocationOperation)editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);

            var newExpression = generator.NameOfExpression(operation.Children.First().Syntax);

            editor.ReplaceNode(nodeToFix, newExpression);
            return editor.GetChangedDocument();
        }
    }
}
