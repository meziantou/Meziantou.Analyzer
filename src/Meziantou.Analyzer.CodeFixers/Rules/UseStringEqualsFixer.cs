using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Simplification;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UseStringEqualsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UseStringEqualsInsteadOfEqualityOperator);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is null)
            return;

        RegisterCodeFix(nameof(StringComparison.Ordinal));
        RegisterCodeFix(nameof(StringComparison.OrdinalIgnoreCase));

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is not null)
        {
            var type = semanticModel.Compilation.GetBestTypeByMetadataName("Meziantou.Framework.StringExtensions");
            if (type is not null)
            {
                if (type.GetMembers("EqualsOrdinal").Length > 0)
                {
                    var title = "Use EqualsOrdinal";
                    var codeAction = CodeAction.Create(
                        title,
                        ct => RefactorExtensionMethod(context.Document, nodeToFix, "EqualsOrdinal", ct),
                        equivalenceKey: title);

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                }

                if (type.GetMembers("EqualsIgnoreCase").Length > 0)
                {
                    var title = "Use EqualsIgnoreCase";
                    var codeAction = CodeAction.Create(
                        title,
                        ct => RefactorExtensionMethod(context.Document, nodeToFix, "EqualsIgnoreCase", ct),
                        equivalenceKey: title);

                    context.RegisterCodeFix(codeAction, context.Diagnostics);
                }
            }
        }

        void RegisterCodeFix(string comparisonMode)
        {
            var title = "Use String.Equals " + comparisonMode;
            var codeAction = CodeAction.Create(
                title,
                ct => RefactorStringEquals(context.Document, nodeToFix, comparisonMode, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> RefactorStringEquals(Document document, SyntaxNode nodeToFix, string comparisonMode, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        var operation = (IBinaryOperation?)semanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation is null)
            return document;

        var stringComparison = semanticModel.Compilation.GetBestTypeByMetadataName("System.StringComparison");
        if (stringComparison is null)
            return document;

        var newExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(generator.TypeExpression(SpecialType.System_String), nameof(string.Equals)),
            operation.LeftOperand.Syntax,
            operation.RightOperand.Syntax,
            generator.MemberAccessExpression(generator.TypeExpression(stringComparison, addImport: true), comparisonMode));

        if (operation.OperatorKind == BinaryOperatorKind.NotEquals)
        {
            newExpression = generator.LogicalNotExpression(newExpression);
        }

        editor.ReplaceNode(nodeToFix, newExpression.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }

    private static async Task<Document> RefactorExtensionMethod(Document document, SyntaxNode nodeToFix, string methodName, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        var operation = (IBinaryOperation?)semanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation is null)
            return document;

        var type = semanticModel.Compilation.GetBestTypeByMetadataName("Meziantou.Framework.StringExtensions");
        if (type is null)
            return document;

        var newExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(generator.TypeExpression(type), methodName),
            operation.LeftOperand.Syntax,
            operation.RightOperand.Syntax)
            .WithAdditionalAnnotations(Simplifier.AddImportsAnnotation, Simplifier.Annotation);

        if (operation.OperatorKind == BinaryOperatorKind.NotEquals)
        {
            newExpression = generator.LogicalNotExpression(newExpression);
        }

        editor.ReplaceNode(nodeToFix, newExpression.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}
