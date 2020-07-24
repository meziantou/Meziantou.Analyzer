using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class MakeMethodStaticFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MakeMethodStatic, RuleIdentifiers.MakePropertyStatic);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix is MethodDeclarationSyntax || nodeToFix is PropertyDeclarationSyntax)
            {
                var title = "Add static modifier";
                var codeAction = CodeAction.Create(
                    title,
                    ct => AddStaticModifier(context.Document, nodeToFix, ct),
                    equivalenceKey: title);

                context.RegisterCodeFix(codeAction, context.Diagnostics);
            }
        }

        private static async Task<Document> AddStaticModifier(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            if (nodeToFix is MethodDeclarationSyntax method)
            {
                editor.ReplaceNode(method, method.WithModifiers(method.Modifiers.Add(SyntaxKind.StaticKeyword)).WithAdditionalAnnotations(Formatter.Annotation));
            }
            else if (nodeToFix is PropertyDeclarationSyntax property)
            {
                editor.ReplaceNode(property, property.WithModifiers(property.Modifiers.Add(SyntaxKind.StaticKeyword)).WithAdditionalAnnotations(Formatter.Annotation));
            }

            return editor.GetChangedDocument();
        }
    }
}
