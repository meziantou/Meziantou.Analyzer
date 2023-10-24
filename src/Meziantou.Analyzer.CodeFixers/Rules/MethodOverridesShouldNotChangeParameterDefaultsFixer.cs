using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Meziantou.Analyzer.Internals;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class MethodOverridesShouldNotChangeParameterDefaultsFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.MethodOverridesShouldNotChangeParameterDefaults);

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (nodeToFix is not ParameterSyntax parameterSyntax)
            return;

        if (!context.Diagnostics[0].Properties.TryGetValue("HasDefaultValue", out var hasValue))
            return;

        if (hasValue == "false")
        {
            var title = "Remove default value";
            var codeAction = CodeAction.Create(
                title,
                ct => RemoveValue(context.Document, parameterSyntax, ct),
                equivalenceKey: title);
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
        else
        {
            if (!context.Diagnostics[0].Properties.TryGetValue("DefaultValue", out var value))
                return;

            var title = "Use parent's default value";
            var codeAction = CodeAction.Create(
                title,
                ct => Refactor(context.Document, parameterSyntax, value, ct),
                equivalenceKey: title);
            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }
    }

    private static async Task<Document> RemoveValue(Document document, ParameterSyntax nodeToFix, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(nodeToFix, nodeToFix.WithDefault(null).WithoutTrailingSpacesTrivia());
        return editor.GetChangedDocument();
    }

    private static async Task<Document> Refactor(Document document, ParameterSyntax nodeToFix, string? value, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        var generator = editor.Generator;

        if (value == null)
        {
            editor.ReplaceNode(nodeToFix, nodeToFix.WithDefault(SyntaxFactory.EqualsValueClause((ExpressionSyntax)generator.NullLiteralExpression())));
        }
        else
        {
            var defaultValue = SyntaxFactory.ParseExpression(value);
            editor.ReplaceNode(nodeToFix, nodeToFix.WithDefault(SyntaxFactory.EqualsValueClause(defaultValue)));
        }

        return editor.GetChangedDocument();
    }
}
