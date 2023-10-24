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
using Microsoft.CodeAnalysis.Formatting;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MakeMemberReadOnlyFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MakeStructMemberReadOnly);

    public override FixAllProvider? GetFixAllProvider() => MakeMemberReadOnlyFixAllProvider.Instance;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix == null)
            return;

        var title = "Add readonly";
        var codeAction = CodeAction.Create(
            title,
            ct => MakeReadOnly(context.Document, nodeToFix, ct),
            equivalenceKey: title);

        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> MakeReadOnly(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (nodeToFix.IsKind(SyntaxKind.PropertyDeclaration))
        {
            var property = (PropertyDeclarationSyntax)nodeToFix;
            editor.ReplaceNode(property, property.WithModifiers(property.Modifiers.Add(SyntaxKind.ReadOnlyKeyword)).WithAdditionalAnnotations(Formatter.Annotation));
        }
        else if (nodeToFix.IsKind(SyntaxKind.MethodDeclaration))
        {
            var method = (MethodDeclarationSyntax)nodeToFix;
            editor.ReplaceNode(method, method.WithModifiers(method.Modifiers.Add(SyntaxKind.ReadOnlyKeyword)).WithAdditionalAnnotations(Formatter.Annotation));
        }
        else if (nodeToFix.IsKind(SyntaxKind.GetAccessorDeclaration) || nodeToFix.IsKind(SyntaxKind.SetAccessorDeclaration) || nodeToFix.IsKind(SyntaxKind.AddAccessorDeclaration) || nodeToFix.IsKind(SyntaxKind.RemoveAccessorDeclaration))
        {
            var accessor = (AccessorDeclarationSyntax)nodeToFix;
            var addToAccessor = false;
            if (accessor.Parent?.IsKind(SyntaxKind.AccessorList) == true)
            {
                var accessorList = (AccessorListSyntax)accessor.Parent;
                var property = (BasePropertyDeclarationSyntax?)accessorList.Parent;
                if (property == null)
                {
                    addToAccessor = true;
                }
                else
                {
                    foreach (var item in accessorList.Accessors)
                    {
                        if (item == accessor)
                            continue;

                        if (!item.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword)))
                        {
                            addToAccessor = true;
                        }
                    }

                    if (!addToAccessor)
                    {
                        foreach (var item in accessorList.Accessors)
                        {
                            accessorList = accessorList.ReplaceNode(item, item.WithModifiers(accessor.Modifiers.Remove(SyntaxKind.ReadOnlyKeyword)));
                        }

                        var newNode = property
                            .WithAccessorList(accessorList)
                            .WithModifiers(property.Modifiers.Add(SyntaxKind.ReadOnlyKeyword));
                        editor.ReplaceNode(property, newNode.WithAdditionalAnnotations(Formatter.Annotation));
                    }
                }
            }

            if (addToAccessor)
            {
                editor.ReplaceNode(accessor, accessor.WithModifiers(accessor.Modifiers.Add(SyntaxKind.ReadOnlyKeyword)).WithAdditionalAnnotations(Formatter.Annotation));
            }
        }

        return editor.GetChangedDocument();
    }

    internal sealed class MakeMemberReadOnlyFixAllProvider : DocumentBasedFixAllProvider
    {
        public static MakeMemberReadOnlyFixAllProvider Instance { get; } = new MakeMemberReadOnlyFixAllProvider();

        protected override string CodeActionTitle => "Add readonly";

        /// <inheritdoc/>
        protected override async Task<SyntaxNode?> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
        {
            if (diagnostics.IsEmpty)
                return null;

            var newDocument = await MakeReadOnly(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);
            return await newDocument.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        internal static async Task<Document> MakeReadOnly(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var nodeToFix = root?.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (nodeToFix == null)
                    continue;

                document = await MakeMemberReadOnlyFixer.MakeReadOnly(document, nodeToFix, cancellationToken).ConfigureAwait(false);
            }

            return document;
        }
    }
}
