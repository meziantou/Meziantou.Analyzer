using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

internal static class ConditionalCompilationBranchesAreIdenticalCommon
{
    internal static bool TryCreateBranchGroup(DirectiveTriviaSyntax directive, SourceText sourceText, [NotNullWhen(true)] out BranchGroup? group)
    {
        var relatedDirectives = directive.GetRelatedDirectives();
        DirectiveTriviaSyntax? firstIfDirective = null;
        DirectiveTriviaSyntax? endIfDirective = null;
        List<DirectiveTriviaSyntax> branchDirectives = [];
        foreach (var relatedDirective in relatedDirectives)
        {
            switch (relatedDirective)
            {
                case IfDirectiveTriviaSyntax:
                    firstIfDirective ??= relatedDirective;
                    branchDirectives.Add(relatedDirective);
                    break;
                case ElifDirectiveTriviaSyntax:
                case ElseDirectiveTriviaSyntax:
                    branchDirectives.Add(relatedDirective);
                    break;
                case EndIfDirectiveTriviaSyntax:
                    endIfDirective = relatedDirective;
                    break;
            }
        }

        if (firstIfDirective is null || endIfDirective is null || branchDirectives.Count < 2)
        {
            group = null;
            return false;
        }

        List<Branch> branches = [];
        for (var i = 0; i < branchDirectives.Count; i++)
        {
            var startDirective = branchDirectives[i];
            var endDirective = i + 1 < branchDirectives.Count ? branchDirectives[i + 1] : endIfDirective;
            var contentSpan = TextSpan.FromBounds(startDirective.FullSpan.End, endDirective.FullSpan.Start);

            var kind = startDirective switch
            {
                IfDirectiveTriviaSyntax => BranchKind.If,
                ElifDirectiveTriviaSyntax => BranchKind.Elif,
                ElseDirectiveTriviaSyntax => BranchKind.Else,
                _ => throw new InvalidOperationException("Unexpected directive kind"),
            };

            string? conditionText = startDirective switch
            {
                IfDirectiveTriviaSyntax ifDirective => ifDirective.Condition.ToString(),
                ElifDirectiveTriviaSyntax elifDirective => elifDirective.Condition.ToString(),
                _ => null,
            };

            branches.Add(new Branch(
                startDirective: startDirective,
                nextDirective: endDirective,
                contentSpan: contentSpan,
                kind: kind,
                conditionText: conditionText,
                signature: ComputeBranchSignature(sourceText, contentSpan)));
        }

        group = new BranchGroup(firstIfDirective, endIfDirective, branches);
        return true;
    }

    private static string ComputeBranchSignature(SourceText sourceText, TextSpan span)
    {
        var text = sourceText.ToString(span);
        var builder = new StringBuilder();
        if (!AppendTokens(builder, text))
            return text.Trim();

        return builder.ToString();
    }

    private static bool AppendTokens(StringBuilder builder, string text)
    {
        var hasToken = false;
        foreach (var token in SyntaxFactory.ParseTokens(text))
        {
            // Preprocessor directives are parsed as trivia, so they must be appended explicitly.
            // Otherwise, two branches containing different nested directives would have the same signature.
            AppendPreprocessorTrivia(builder, token.LeadingTrivia);
            if (token.RawKind != (int)SyntaxKind.EndOfFileToken)
            {
                hasToken = true;
                builder.Append(token.RawKind);
                builder.Append(':');
                builder.Append(token.Text);
                builder.Append(';');
            }

            AppendPreprocessorTrivia(builder, token.TrailingTrivia);
        }

        return hasToken;
    }

    private static void AppendPreprocessorTrivia(StringBuilder builder, SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            if (trivia.IsDirective)
            {
                builder.Append('#');
                builder.Append(trivia.ToString().Trim());
                builder.Append(';');
            }
            else if (trivia.RawKind == (int)SyntaxKind.DisabledTextTrivia)
            {
                // The text of an inactive nested branch is not tokenized, so it is tokenized separately
                builder.Append('~');
                AppendTokens(builder, trivia.ToString());
                builder.Append(';');
            }
        }
    }

    internal sealed class BranchGroup(
        DirectiveTriviaSyntax firstIfDirective,
        DirectiveTriviaSyntax endIfDirective,
        IReadOnlyList<Branch> branches)
    {
        internal DirectiveTriviaSyntax FirstIfDirective { get; } = firstIfDirective;
        internal DirectiveTriviaSyntax EndIfDirective { get; } = endIfDirective;
        internal IReadOnlyList<Branch> Branches { get; } = branches;

        internal int GetBranchIndex(DirectiveTriviaSyntax directive)
        {
            for (var i = 0; i < Branches.Count; i++)
            {
                if (Branches[i].StartDirective.SpanStart == directive.SpanStart)
                    return i;
            }

            return -1;
        }

        internal int FindPreviousDuplicateBranchIndex(int currentBranchIndex)
        {
            var signature = Branches[currentBranchIndex].Signature;
            for (var i = 0; i < currentBranchIndex; i++)
            {
                if (string.Equals(Branches[i].Signature, signature, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }
    }

    internal sealed class Branch(
        DirectiveTriviaSyntax startDirective,
        DirectiveTriviaSyntax nextDirective,
        TextSpan contentSpan,
        BranchKind kind,
        string? conditionText,
        string signature)
    {
        internal DirectiveTriviaSyntax StartDirective { get; } = startDirective;
        internal DirectiveTriviaSyntax NextDirective { get; } = nextDirective;
        internal TextSpan ContentSpan { get; } = contentSpan;
        internal BranchKind Kind { get; } = kind;
        internal string? ConditionText { get; } = conditionText;
        internal string Signature { get; } = signature;
    }

    internal enum BranchKind
    {
        If,
        Elif,
        Else,
    }
}
