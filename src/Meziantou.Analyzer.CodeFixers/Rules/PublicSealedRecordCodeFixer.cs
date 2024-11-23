using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PublicRecordCodeFixer)), Shared]
    public class PublicRecordCodeFixer : CodeFixProvider
    {
        private const string SealedRecordTitle = "Annotate public record with 'sealed'";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => [RuleIdentifiers.PublicRecordShouldBeSealed];

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf()
                .OfType<RecordDeclarationSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create(
                    SealedRecordTitle,
                    _ => AddSealedKeyword(context.Document, declaration),
                    SealedRecordTitle),
                diagnostic);
        }

        private static async Task<Document> AddSealedKeyword(Document document, RecordDeclarationSyntax expression)
        {
            var newexpression = expression.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
            var root = await document.GetSyntaxRootAsync();
            return document.WithSyntaxRoot(root.ReplaceNode(expression, newexpression));
        }
    }
}