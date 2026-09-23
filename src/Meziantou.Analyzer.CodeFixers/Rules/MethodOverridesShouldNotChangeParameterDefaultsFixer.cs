using Microsoft.CodeAnalysis.Simplification;

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

        if (!context.Diagnostics[0].Properties.TryGetValue(MethodOverridesShouldNotChangeParameterDefaultsAnalyzerCommon.HasDefaultValueKey, out var hasValue))
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
            // The value is null when the analyzer cannot create an expression for the default value of the parameter
            if (!context.Diagnostics[0].Properties.TryGetValue(MethodOverridesShouldNotChangeParameterDefaultsAnalyzerCommon.DefaultValueKey, out var value) || value is null)
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

    private static async Task<Document> Refactor(Document document, ParameterSyntax nodeToFix, string value, CancellationToken cancellationToken)
    {
        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

        // The expressions created from a referenced assembly use fully qualified names (global::Namespace.Enum.Member)
        // The type of a cast is not simplified when only the cast expression is annotated
        var defaultValue = SyntaxFactory.ParseExpression(value);
        defaultValue = defaultValue
            .ReplaceNodes(defaultValue.DescendantNodes().OfType<TypeSyntax>(), (_, type) => type.WithAdditionalAnnotations(Simplifier.Annotation))
            .WithAdditionalAnnotations(Simplifier.Annotation);
        editor.ReplaceNode(nodeToFix, nodeToFix.WithDefault(SyntaxFactory.EqualsValueClause(defaultValue)));
        return editor.GetChangedDocument();
    }
}
