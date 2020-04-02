using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public sealed class ClassWithEqualsTShouldImplementIEquatableTFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ClassWithEqualsTShouldImplementIEquatableT);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Implement System.IEquatable";
            var codeAction = CodeAction.Create(
                title,
                ct => ImplementIEquatable(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> ImplementIEquatable(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            if (!(nodeToFix is BaseTypeDeclarationSyntax typeDeclarationSyntax))
                return document;

            var interfaceTypeSyntax = SimpleBaseType(
                QualifiedName(IdentifierName("System"), GenericName(Identifier("IEquatable"))
                    .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(typeDeclarationSyntax.Identifier.WithoutTrivia()))))))
                .WithAdditionalAnnotations(Simplifier.Annotation);

            BaseTypeDeclarationSyntax newTypeDeclarationSyntax;
            if (typeDeclarationSyntax.BaseList is null)
            {
                newTypeDeclarationSyntax = typeDeclarationSyntax
                    .WithIdentifier(typeDeclarationSyntax.Identifier.WithTrailingTrivia(null))
                    .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(interfaceTypeSyntax))
                        .WithTrailingTrivia(typeDeclarationSyntax.Identifier.TrailingTrivia));
            }
            else
            {
                newTypeDeclarationSyntax = typeDeclarationSyntax
                    .AddBaseListTypes(interfaceTypeSyntax);
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(typeDeclarationSyntax, newTypeDeclarationSyntax);

            return editor.GetChangedDocument();
        }
    }
}
