﻿using System;
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

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class DoNotUseEqualityOperatorsForSpanOfCharFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseEqualityOperatorsForSpanOfChar);

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

        RegisterCodeFix(nameof(StringComparison.Ordinal));
        RegisterCodeFix(nameof(StringComparison.OrdinalIgnoreCase));

        void RegisterCodeFix(string comparisonMode)
        {
            var title = "Use SequenceEquals " + comparisonMode;
            var codeAction = CodeAction.Create(
                title,
                ct => Refactor(context.Document, nodeToFix, comparisonMode, ct),
                equivalenceKey: title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> Refactor(Document document, SyntaxNode nodeToFix, string comparisonMode, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = editor.SemanticModel;
        var generator = editor.Generator;

        var operation = (IBinaryOperation?)semanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation == null)
            return document;

        var stringComparison = semanticModel.Compilation.GetBestTypeByMetadataName("System.StringComparison");
        if (stringComparison is null)
            return document;

        var newExpression = generator.InvocationExpression(
            generator.MemberAccessExpression(operation.LeftOperand.Syntax, "SequenceEqual"),
            operation.RightOperand.Syntax,
            generator.MemberAccessExpression(generator.TypeExpression(stringComparison, addImport: true), comparisonMode));

        if (operation.OperatorKind == BinaryOperatorKind.NotEquals)
        {
            newExpression = generator.LogicalNotExpression(newExpression);
        }

        editor.ReplaceNode(nodeToFix, newExpression.WithAdditionalAnnotations(Formatter.Annotation));
        return editor.GetChangedDocument();
    }
}
