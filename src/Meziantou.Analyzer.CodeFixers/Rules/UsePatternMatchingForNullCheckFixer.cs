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
using Microsoft.CodeAnalysis.Operations;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class UsePatternMatchingForNullCheckFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.UsePatternMatchingForNullCheck, RuleIdentifiers.UsePatternMatchingForNullEquality);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not BinaryExpressionSyntax invocation)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use pattern matching",
                ct => Update(context.Document, invocation, ct),
                equivalenceKey: "Use pattern matching"),
            context.Diagnostics);
    }

    private static async Task<Document> Update(Document document, BinaryExpressionSyntax node, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        if (editor.SemanticModel.GetOperation(node, cancellationToken) is not IBinaryOperation operation)
            return document;

        var valueSyntax = IsNull(operation.LeftOperand) ? operation.RightOperand.Syntax : operation.LeftOperand.Syntax;
        if (valueSyntax is not ExpressionSyntax expression)
            return document;

        PatternSyntax constantExpression = ConstantPattern(LiteralExpression(SyntaxKind.NullLiteralExpression));
        if (operation.OperatorKind is BinaryOperatorKind.NotEquals)
        {
            constantExpression = UnaryPattern(constantExpression);
        }

        var newSyntax = IsPatternExpression(expression, constantExpression);
        editor.ReplaceNode(node, newSyntax);
        return editor.GetChangedDocument();
    }

    private static bool IsNull(IOperation operation)
        => operation.UnwrapConversionOperations() is ILiteralOperation { ConstantValue: { HasValue: true, Value: null } };
}
