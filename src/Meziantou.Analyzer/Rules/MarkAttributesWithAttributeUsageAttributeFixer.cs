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
    public sealed class MarkAttributesWithAttributeUsageAttributeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MarkAttributesWithAttributeUsageAttribute);

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix == null)
                return;

            var title = "Add AttributeUsage attribute";
            var codeAction = CodeAction.Create(
                title,
                ct => Refactor(context.Document, nodeToFix, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static async Task<Document> Refactor(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var semanticModel = editor.SemanticModel;
            var generator = editor.Generator;

            var classNode = (ClassDeclarationSyntax)nodeToFix;

            var attributeUsageAttribute = semanticModel.Compilation.GetTypeByMetadataName("System.AttributeUsageAttribute");
            var attributeTargets = semanticModel.Compilation.GetTypeByMetadataName("System.AttributeTargets");

            var attribute = editor.Generator.Attribute(
                generator.TypeExpression(attributeUsageAttribute, addImport: true),
                new[]
                {
                    generator.AttributeArgument(
                        generator.MemberAccessExpression(
                            generator.TypeExpression(attributeTargets, addImport: true),
                            nameof(AttributeTargets.All))),
                });

            editor.AddAttribute(classNode, attribute);
            return editor.GetChangedDocument();
        }
    }
}
