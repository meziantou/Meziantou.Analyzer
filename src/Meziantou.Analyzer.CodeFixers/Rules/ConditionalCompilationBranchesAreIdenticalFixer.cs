using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ConditionalCompilationBranchesAreIdenticalFixer : CodeFixProvider
{
    private const string MergeTitle = "Merge duplicate conditional compilation branches";
    private const string RemoveTitle = "Remove redundant conditional compilation block";

    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.ConditionalCompilationBranchesAreIdentical);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
            return;

        var sourceText = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
        if (!TryGetFixContext(root, sourceText, context.Span, out var fixContext))
            return;

        var currentBranch = fixContext.Group.Branches[fixContext.CurrentBranchIndex];
        var duplicateBranch = fixContext.Group.Branches[fixContext.DuplicateBranchIndex];
        if (currentBranch.Kind == ConditionalCompilationBranchesAreIdenticalCommon.BranchKind.Else &&
            duplicateBranch.Kind == ConditionalCompilationBranchesAreIdenticalCommon.BranchKind.If &&
            fixContext.Group.Branches.Count == 2 &&
            fixContext.CurrentBranchIndex == 1)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    RemoveTitle,
                    ct => RemoveRedundantConditionalCompilationAsync(context.Document, context.Diagnostics[0], ct),
                    equivalenceKey: RemoveTitle),
                context.Diagnostics);
        }
        else if (currentBranch.Kind == ConditionalCompilationBranchesAreIdenticalCommon.BranchKind.Elif &&
                 duplicateBranch.Kind is ConditionalCompilationBranchesAreIdenticalCommon.BranchKind.If or ConditionalCompilationBranchesAreIdenticalCommon.BranchKind.Elif &&
                 currentBranch.ConditionText is not null &&
                 duplicateBranch.ConditionText is not null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    MergeTitle,
                    ct => MergeDuplicateConditionalCompilationBranchAsync(context.Document, context.Diagnostics[0], ct),
                    equivalenceKey: MergeTitle),
                context.Diagnostics);
        }
    }

    private static async Task<Document> RemoveRedundantConditionalCompilationAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetFixContext(root, sourceText, diagnostic.Location.SourceSpan, out var fixContext))
            return document;

        var firstBranch = fixContext.Group.Branches[0];
        var replacementText = sourceText.ToString(firstBranch.ContentSpan);
        var replacementSpan = TextSpan.FromBounds(fixContext.Group.FirstIfDirective.FullSpan.Start, fixContext.Group.EndIfDirective.FullSpan.End);
        var updatedText = sourceText.WithChanges(new TextChange(replacementSpan, replacementText));
        return document.WithText(updatedText);
    }

    private static async Task<Document> MergeDuplicateConditionalCompilationBranchAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
            return document;

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        if (!TryGetFixContext(root, sourceText, diagnostic.Location.SourceSpan, out var fixContext))
            return document;

        var currentBranch = fixContext.Group.Branches[fixContext.CurrentBranchIndex];
        var duplicateBranch = fixContext.Group.Branches[fixContext.DuplicateBranchIndex];
        if (currentBranch.Kind != ConditionalCompilationBranchesAreIdenticalCommon.BranchKind.Elif ||
            currentBranch.ConditionText is null ||
            duplicateBranch.ConditionText is null)
        {
            return document;
        }

        var mergedCondition = $"({duplicateBranch.ConditionText}) || ({currentBranch.ConditionText})";
        var mergedDirective = duplicateBranch.Kind switch
        {
            ConditionalCompilationBranchesAreIdenticalCommon.BranchKind.If => "#if " + mergedCondition,
            ConditionalCompilationBranchesAreIdenticalCommon.BranchKind.Elif => "#elif " + mergedCondition,
            _ => null,
        };

        if (mergedDirective is null)
            return document;

        var removeBranchSpan = TextSpan.FromBounds(currentBranch.StartDirective.FullSpan.Start, currentBranch.NextDirective.FullSpan.Start);
        var updatedText = sourceText.WithChanges(
            new TextChange(duplicateBranch.StartDirective.Span, mergedDirective),
            new TextChange(removeBranchSpan, string.Empty));

        return document.WithText(updatedText);
    }

    private static bool TryGetFixContext(SyntaxNode root, SourceText sourceText, TextSpan span, out FixContext fixContext)
    {
        var directive = FindDirective(root, span);
        if (directive is null)
        {
            fixContext = default;
            return false;
        }

        if (!ConditionalCompilationBranchesAreIdenticalCommon.TryCreateBranchGroup(directive, sourceText, out var branchGroup))
        {
            fixContext = default;
            return false;
        }

        var currentBranchIndex = branchGroup.GetBranchIndex(directive);
        if (currentBranchIndex <= 0)
        {
            fixContext = default;
            return false;
        }

        var duplicateBranchIndex = branchGroup.FindPreviousDuplicateBranchIndex(currentBranchIndex);
        if (duplicateBranchIndex < 0)
        {
            fixContext = default;
            return false;
        }

        fixContext = new FixContext(branchGroup, currentBranchIndex, duplicateBranchIndex);
        return true;
    }

    private static DirectiveTriviaSyntax? FindDirective(SyntaxNode root, TextSpan span)
    {
        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            if (!trivia.HasStructure || !trivia.FullSpan.Contains(span.Start))
                continue;

            if (trivia.GetStructure() is DirectiveTriviaSyntax directive)
                return directive;
        }

        return null;
    }

    private readonly record struct FixContext(ConditionalCompilationBranchesAreIdenticalCommon.BranchGroup Group, int CurrentBranchIndex, int DuplicateBranchIndex);
}
