using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class DoNotUseEqualityComparerDefaultOfStringFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseEqualityComparerDefaultOfString);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            RegisterCodeFix(nameof(StringComparer.Ordinal));
            RegisterCodeFix(nameof(StringComparer.OrdinalIgnoreCase));
            
            void RegisterCodeFix(string comparerName)
            {
                var title = "Use StringComparer." + comparerName;
                var codeAction = CodeAction.Create(
                    title,
                    ct => MakeConstructorProtected(context.Document, nodeToFix, comparerName, ct),
                    equivalenceKey: title);

                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }

        private static async Task<Document> MakeConstructorProtected(Document document, SyntaxNode nodeToFix, string comparerName, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = editor.SemanticModel;
            var generator = editor.Generator;

            var syntax = (MemberAccessExpressionSyntax)nodeToFix;

            var stringComparer = semanticModel.Compilation.GetTypeByMetadataName("System.StringComparer");

            var newSyntax = generator.MemberAccessExpression(
                generator.TypeExpression(stringComparer),
                comparerName);

            editor.ReplaceNode(syntax, newSyntax);
            return editor.GetChangedDocument();
        }
    }
}
