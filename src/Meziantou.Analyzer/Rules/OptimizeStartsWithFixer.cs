using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class OptimizeStartsWithFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.OptimizeStartsWith);

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

        var title = "Optimize arguments";
        var codeAction = CodeAction.Create(
            title,
            ct => Fix(context.Document, nodeToFix, ct),
            equivalenceKey: title);
        context.RegisterCodeFix(codeAction, context.Diagnostics);
    }

    private static async Task<Document> Fix(Document document, SyntaxNode nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var operation = editor.SemanticModel.GetOperation(nodeToFix, cancellationToken);
        if (operation is ILiteralOperation literalOperation && literalOperation.ConstantValue.Value is string { Length: 1 } literalValue)
        {
            editor.ReplaceNode(literalOperation.Syntax, editor.Generator.LiteralExpression(literalValue[0]));
        }
        else if (operation is IArgumentOperation argumentOperation && argumentOperation.Value.ConstantValue.Value is string { Length: 1 } argumentValue)
        {
            editor.ReplaceNode(argumentOperation.Value.Syntax, editor.Generator.LiteralExpression(argumentValue[0]));
        }
        else if (operation is IInvocationOperation invocation && invocation.TargetMethod.Name == "Replace" && invocation.Arguments.Length >= 2)
        {
            for (var i = 0; i < 2; i++)
            {
                if (invocation.Arguments[i].Value.ConstantValue.Value is string { Length: 1 } arg)
                {
                    editor.ReplaceNode(invocation.Arguments[i].Value.Syntax, editor.Generator.LiteralExpression(arg[0]));
                }
            }
        }

        return editor.GetChangedDocument();
    }
}
