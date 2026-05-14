using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class DoNotUseEmptyPropertyPatternOnNonNullableValueTypeFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.DoNotUseEmptyPropertyPatternOnNonNullableValueType);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
        if (TryGetFixableRecursivePattern(nodeToFix) is not { } recursivePattern)
            return;

        const string Title = "Use var pattern";
        context.RegisterCodeFix(
            CodeAction.Create(Title, ct => UseVarPattern(context.Document, recursivePattern, ct), equivalenceKey: Title),
            context.Diagnostics);
    }

    private static RecursivePatternSyntax? TryGetFixableRecursivePattern(SyntaxNode? node)
    {
        var recursivePattern = node as RecursivePatternSyntax ?? node?.FirstAncestorOrSelf<RecursivePatternSyntax>();
        if (recursivePattern is null)
            return null;

        if (!IsFixablePattern(recursivePattern))
            return null;

        return recursivePattern;
    }

    private static bool IsFixablePattern(RecursivePatternSyntax recursivePattern)
    {
        return recursivePattern is
        {
            Type: null,
            PositionalPatternClause: null,
            Designation: not null,
            PropertyPatternClause.Subpatterns.Count: 0,
        };
    }

    private static async Task<Document> UseVarPattern(Document document, RecursivePatternSyntax recursivePattern, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var currentNode = root.FindNode(recursivePattern.Span, getInnermostNodeForTie: true);
        if (TryGetFixableRecursivePattern(currentNode) is not { } currentPattern)
            return document;

        var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
        editor.ReplaceNode(currentPattern, CreateReplacementPattern(currentPattern));
        return editor.GetChangedDocument();
    }

    private static VarPatternSyntax CreateReplacementPattern(RecursivePatternSyntax recursivePattern)
    {
        return VarPattern(recursivePattern.Designation!).WithTriviaFrom(recursivePattern);
    }
}
