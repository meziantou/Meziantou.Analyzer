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

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class AbstractTypesShouldNotHaveConstructorsFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.AbstractTypesShouldNotHaveConstructors);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Make constructor protected";
            var codeAction = CodeAction.Create(
                title,
                ct => MakeConstructorProtected(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> MakeConstructorProtected(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            var ctorSyntax = (ConstructorDeclarationSyntax)nodeToFix;
            if (ctorSyntax == null)
                return document;

            var modifiers = ctorSyntax.Modifiers;
            foreach (var modifier in modifiers.Where(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword)))
            {
                modifiers = modifiers.Remove(modifier);
            }

            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));

            editor.ReplaceNode(ctorSyntax, ctorSyntax.WithModifiers(modifiers));
            return editor.GetChangedDocument();
        }
    }
}
