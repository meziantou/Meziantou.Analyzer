using System;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class OptimizeStringBuilderUsageFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.OptimizeStringBuilderUsage);

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

        var diagnostic = context.Diagnostics.FirstOrDefault();
        if (diagnostic == null)
            return;

        if (!Enum.TryParse(diagnostic.Properties.GetValueOrDefault("Data", ""), out OptimizeStringBuilderUsageData data) || data == OptimizeStringBuilderUsageData.None)
            return;

        var title = "Optimize StringBuilder usage";
        switch (data)
        {
            case OptimizeStringBuilderUsageData.RemoveArgument:
                context.RegisterCodeFix(CodeAction.Create(title, ct => RemoveArgument(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeStringBuilderUsageData.RemoveMethod:
                context.RegisterCodeFix(CodeAction.Create(title, ct => RemoveMethod(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeStringBuilderUsageData.ReplaceWithChar:
                context.RegisterCodeFix(CodeAction.Create(title, ct => ReplaceArgWithCharacter(context.Document, diagnostic, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeStringBuilderUsageData.SplitStringInterpolation:
                context.RegisterCodeFix(CodeAction.Create(title, ct => SplitStringInterpolation(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeStringBuilderUsageData.SplitAddOperator:
                context.RegisterCodeFix(CodeAction.Create(title, ct => SplitAddOperator(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeStringBuilderUsageData.RemoveToString:
                context.RegisterCodeFix(CodeAction.Create(title, ct => RemoveToString(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeStringBuilderUsageData.ReplaceWithAppendFormat:
                context.RegisterCodeFix(CodeAction.Create(title, ct => ReplaceWithAppendFormat(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;

            case OptimizeStringBuilderUsageData.ReplaceSubstring:
                context.RegisterCodeFix(CodeAction.Create(title, ct => ReplaceSubstring(context.Document, nodeToFix, ct), equivalenceKey: title), context.Diagnostics);
                break;
        }
    }

    private static async Task<Document> SplitStringInterpolation(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var operation = (IInvocationOperation?)editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation == null)
            return document;

        var methodName = operation.TargetMethod.Name; // Append or AppendLine
        var argument = (IInterpolatedStringOperation)operation.Arguments[0].Value;

        var shouldAppendLastAppendLine = string.Equals(methodName, nameof(StringBuilder.AppendLine), StringComparison.Ordinal);
        var newExpression = operation.GetChildOperations().First().Syntax;
        foreach (var part in argument.Parts)
        {
            if (part is IInterpolatedStringTextOperation str)
            {
                var text = OptimizeStringBuilderUsageAnalyzer.GetConstStringValue(str);
                if (text == null)
                    return document; // This should not happen

                var newArgument = generator.LiteralExpression(text.Length == 1 ? text[0] : text);
                if (shouldAppendLastAppendLine && part == argument.Parts.Last())
                {
                    if (text.Length == 1)
                    {
                        // AppendLine doesn't support char, so we need to use Append(char).AppendLine();
                        newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "Append"), newArgument);
                        newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "AppendLine"));
                    }
                    else
                    {
                        newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "AppendLine"), newArgument);
                    }

                    shouldAppendLastAppendLine = false;
                }
                else
                {
                    newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "Append"), newArgument);
                }
            }
            else if (part is IInterpolationOperation interpolation)
            {
                if (interpolation.FormatString == null && interpolation.Alignment == null)
                {
                    newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "Append"), interpolation.Expression.Syntax);
                }
                else
                {
                    var format = "{0";
                    if (interpolation.Alignment != null)
                    {
                        var value = interpolation.Alignment.ConstantValue.Value;
                        format += "," + string.Format(CultureInfo.InvariantCulture, "{0}", value);
                    }

                    if (interpolation.FormatString != null)
                    {
                        format += ":" + OptimizeStringBuilderUsageAnalyzer.GetConstStringValue(interpolation.FormatString);
                    }

                    format += "}";
                    newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "AppendFormat"), generator.LiteralExpression(format), interpolation.Expression.Syntax);
                }
            }
        }

        if (shouldAppendLastAppendLine)
        {
            newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "AppendLine"));
        }

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> SplitAddOperator(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var operation = (IInvocationOperation?)editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation == null)
            return document;

        var methodName = operation.TargetMethod.Name; // Append or AppendLine
        var isAppendLine = string.Equals(methodName, nameof(StringBuilder.AppendLine), StringComparison.Ordinal);

        var binaryOperation = (IBinaryOperation)operation.Arguments[0].Value;

        var newExpression = generator.InvocationExpression(generator.MemberAccessExpression(operation.GetChildOperations().First().Syntax, "Append"), binaryOperation.LeftOperand.Syntax);
        newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, isAppendLine ? "AppendLine" : "Append"), binaryOperation.RightOperand.Syntax);

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> RemoveToString(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var operation = (IInvocationOperation?)editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation == null)
            return document;

        var methodName = operation.TargetMethod.Name; // Append or AppendLine
        var isAppendLine = string.Equals(methodName, nameof(StringBuilder.AppendLine), StringComparison.Ordinal);

        var toStringOperation = (IInvocationOperation)operation.Arguments[0].Value;

        var newExpression = generator.InvocationExpression(generator.MemberAccessExpression(operation.GetChildOperations().First().Syntax, "Append"), toStringOperation.GetChildOperations().First().Syntax);
        if (isAppendLine)
        {
            newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "AppendLine"));
        }

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> ReplaceWithAppendFormat(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var operation = (IInvocationOperation?)editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation == null)
            return document;

        var methodName = operation.TargetMethod.Name; // Append or AppendLine
        var isAppendLine = string.Equals(methodName, nameof(StringBuilder.AppendLine), StringComparison.Ordinal);

        var toStringOperation = (IInvocationOperation)operation.Arguments[0].Value;

        var newExpression = generator.InvocationExpression(generator.MemberAccessExpression(operation.GetChildOperations().First().Syntax, "AppendFormat"),
            toStringOperation.Arguments[1].Syntax,
            GetFormatExpression(toStringOperation.Arguments[0].Value),
            toStringOperation.GetChildOperations().First().Syntax);

        if (isAppendLine)
        {
            newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "AppendLine"));
        }

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();

        SyntaxNode GetFormatExpression(IOperation formatOperation)
        {
            if (formatOperation.ConstantValue.HasValue)
            {
                return generator.LiteralExpression("{0:" + (string?)formatOperation.ConstantValue.Value + "}");
            }

            return generator.AddExpression(generator.AddExpression(
                generator.LiteralExpression("{0:"),
                formatOperation.Syntax),
                generator.LiteralExpression("}"));
        }
    }

    private static async Task<Document> ReplaceSubstring(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;
        var operation = (IInvocationOperation?)editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation == null)
            return document;

        var methodName = operation.TargetMethod.Name; // Append or AppendLine
        var isAppendLine = string.Equals(methodName, nameof(StringBuilder.AppendLine), StringComparison.Ordinal);

        var toStringOperation = (IInvocationOperation)operation.Arguments[0].Value;

        var strSyntax = toStringOperation.GetChildOperations().First().Syntax;
        var lengthArgument = toStringOperation.Arguments.Length == 2 ?
            toStringOperation.Arguments[1].Value.Syntax :
            generator.SubtractExpression(generator.MemberAccessExpression(strSyntax, "Length"), toStringOperation.Arguments[0].Value.Syntax);

        var newExpression = generator.InvocationExpression(
                generator.MemberAccessExpression(operation.GetChildOperations().First().Syntax, "Append"),
                strSyntax,
                toStringOperation.Arguments[0].Value.Syntax,
                lengthArgument);

        if (isAppendLine)
        {
            newExpression = generator.InvocationExpression(generator.MemberAccessExpression(newExpression, "AppendLine"));
        }

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> ReplaceArgWithCharacter(Document document, Diagnostic diagnostic, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var constValue = diagnostic.Properties["ConstantValue"]![0];
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var argument = nodeToFix.FirstAncestorOrSelf<ArgumentSyntax>();
        if (argument != null)
        {
            var newArgument = argument.WithExpression((ExpressionSyntax)editor.Generator.LiteralExpression(constValue));
            editor.ReplaceNode(argument, newArgument);
        }

        return editor.GetChangedDocument();
    }

    private static async Task<Document> RemoveArgument(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newExpression = ((InvocationExpressionSyntax)nodeToFix).WithArgumentList(SyntaxFactory.ArgumentList());

        editor.ReplaceNode(nodeToFix, newExpression);
        return editor.GetChangedDocument();
    }

    private static async Task<Document> RemoveMethod(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        var newExpression = (InvocationExpressionSyntax)nodeToFix;
        if (newExpression.Expression is MemberAccessExpressionSyntax expression)
        {
            editor.ReplaceNode(nodeToFix, expression.Expression);
        }

        return editor.GetChangedDocument();
    }
}
