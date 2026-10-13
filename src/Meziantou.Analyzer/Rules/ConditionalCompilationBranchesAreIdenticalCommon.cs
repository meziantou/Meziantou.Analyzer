using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

internal static class ConditionalCompilationBranchesAreIdenticalCommon
{
    // The kind of the parts that are not tokens. The kind of a token is its RawKind, which is positive.
    private const int DirectivePartKind = -1;
    private const int DisabledTextStartPartKind = -2;
    private const int DisabledTextEndPartKind = -3;

    /// <summary>Gets the <c>#if</c> blocks of the tree that have at least two branches, the nested blocks first.</summary>
    internal static IEnumerable<BranchGroup> GetBranchGroups(SyntaxNode root, SourceText sourceText)
    {
        // The branch directives of the blocks that are not closed yet. blockStarts contains the index of the #if of each of them.
        // The blocks are matched in a single pass, as getting the related directives of each #if enumerates the directives
        // of its nested blocks again.
        List<DirectiveTriviaSyntax> branchDirectives = [];
        Stack<int> blockStarts = new();
        foreach (var item in root.DescendantNodesAndTokens(static node => node.ContainsDirectives, descendIntoTrivia: true))
        {
            switch (item.AsNode())
            {
                case IfDirectiveTriviaSyntax ifDirective:
                    blockStarts.Push(branchDirectives.Count);
                    branchDirectives.Add(ifDirective);
                    break;

                case BranchingDirectiveTriviaSyntax branchDirective when blockStarts.Count > 0:
                    branchDirectives.Add(branchDirective);
                    break;

                case EndIfDirectiveTriviaSyntax endIfDirective when blockStarts.Count > 0:
                    var blockStart = blockStarts.Pop();
                    if (branchDirectives.Count - blockStart >= 2)
                        yield return new BranchGroup(CreateBranches(branchDirectives, blockStart, endIfDirective), endIfDirective, sourceText);

                    branchDirectives.RemoveRange(blockStart, branchDirectives.Count - blockStart);
                    break;
            }
        }
    }

    private static Branch[] CreateBranches(List<DirectiveTriviaSyntax> branchDirectives, int blockStart, EndIfDirectiveTriviaSyntax endIfDirective)
    {
        var branches = new Branch[branchDirectives.Count - blockStart];
        for (var i = 0; i < branches.Length; i++)
        {
            var index = blockStart + i;
            branches[i] = new Branch(branchDirectives[index], index + 1 < branchDirectives.Count ? branchDirectives[index + 1] : endIfDirective);
        }

        return branches;
    }

    // Preprocessor directives are parsed as trivia, so they must be compared explicitly.
    // Otherwise, two branches containing different nested directives would be considered as identical.
    private static IEnumerable<(int Kind, string Text)> EnumerateParts(string text)
    {
        foreach (var token in SyntaxFactory.ParseTokens(text))
        {
            if (token.ContainsDirectives)
            {
                foreach (var part in EnumeratePreprocessorParts(token.LeadingTrivia))
                    yield return part;
            }

            if (token.RawKind != (int)SyntaxKind.EndOfFileToken)
                yield return (token.RawKind, token.Text);

            if (token.ContainsDirectives)
            {
                foreach (var part in EnumeratePreprocessorParts(token.TrailingTrivia))
                    yield return part;
            }
        }
    }

    private static IEnumerable<(int Kind, string Text)> EnumeratePreprocessorParts(SyntaxTriviaList triviaList)
    {
        foreach (var trivia in triviaList)
        {
            if (trivia.IsDirective)
            {
                yield return (DirectivePartKind, trivia.ToString().Trim());
            }
            else if (trivia.RawKind == (int)SyntaxKind.DisabledTextTrivia)
            {
                // The text of an inactive nested branch is not tokenized, so it is tokenized separately
                yield return (DisabledTextStartPartKind, "");
                foreach (var part in EnumerateParts(trivia.ToString()))
                    yield return part;

                yield return (DisabledTextEndPartKind, "");
            }
        }
    }

    internal sealed class BranchGroup(Branch[] branches, EndIfDirectiveTriviaSyntax endIfDirective, SourceText sourceText)
    {
        internal DirectiveTriviaSyntax FirstIfDirective => Branches[0].StartDirective;
        internal EndIfDirectiveTriviaSyntax EndIfDirective { get; } = endIfDirective;
        internal IReadOnlyList<Branch> Branches { get; } = branches;

        /// <summary>Gets, for each branch, the index of the first branch of the group that has the same content.</summary>
        internal int[] GetFirstBranchesWithSameContent()
        {
            var result = new int[Branches.Count];
            var contents = new BranchContent?[Branches.Count];
            try
            {
                for (var i = 0; i < contents.Length; i++)
                {
                    var content = new BranchContent(sourceText.ToString(Branches[i].ContentSpan));
                    contents[i] = content;
                    result[i] = i;
                    for (var j = 0; j < i; j++)
                    {
                        if (result[j] == j && contents[j]!.HasSameContent(content))
                        {
                            result[i] = j;
                            break;
                        }
                    }
                }
            }
            finally
            {
                foreach (var content in contents)
                {
                    content?.Dispose();
                }
            }

            return result;
        }
    }

    internal sealed class Branch(DirectiveTriviaSyntax startDirective, DirectiveTriviaSyntax nextDirective)
    {
        internal DirectiveTriviaSyntax StartDirective { get; } = startDirective;
        internal DirectiveTriviaSyntax NextDirective { get; } = nextDirective;
        internal TextSpan ContentSpan => TextSpan.FromBounds(StartDirective.FullSpan.End, NextDirective.FullSpan.Start);

        internal BranchKind Kind => StartDirective switch
        {
            IfDirectiveTriviaSyntax => BranchKind.If,
            ElifDirectiveTriviaSyntax => BranchKind.Elif,
            ElseDirectiveTriviaSyntax => BranchKind.Else,
            _ => throw new InvalidOperationException("Unexpected directive kind"),
        };

        internal string? ConditionText => (StartDirective as ConditionalDirectiveTriviaSyntax)?.Condition.ToString();
    }

    internal enum BranchKind
    {
        If,
        Elif,
        Else,
    }

    /// <summary>
    /// The content of a branch, which is tokenized on demand. Most of the branches differ in their first tokens,
    /// so comparing them does not require tokenizing them entirely.
    /// </summary>
    private sealed class BranchContent(string text) : IDisposable
    {
        private readonly List<(int Kind, string Text)> _parts = [];
        private IEnumerator<(int Kind, string Text)>? _enumerator = EnumerateParts(text).GetEnumerator();
        private int _disabledTextDepth;
        private bool _hasToken;

        public bool HasSameContent(BranchContent other)
        {
            for (var i = 0; ; i++)
            {
                var hasPart = TryGetPart(i, out var part);
                if (hasPart != other.TryGetPart(i, out var otherPart))
                    return false;

                if (!hasPart)
                    break;

                if (part.Kind != otherPart.Kind || !string.Equals(part.Text, otherPart.Text, StringComparison.Ordinal))
                    return false;
            }

            // Both branches are entirely tokenized here, and they have the same tokens and directives.
            // The comments are not tokens, so they are compared textually when the branches do not contain any code.
            return _hasToken || text.AsSpan().Trim().SequenceEqual(other.Text.AsSpan().Trim());
        }

        private string Text => text;

        private bool TryGetPart(int index, out (int Kind, string Text) part)
        {
            while (index >= _parts.Count)
            {
                if (_enumerator is null)
                {
                    part = default;
                    return false;
                }

                if (_enumerator.MoveNext())
                {
                    var current = _enumerator.Current;
                    _parts.Add(current);
                    switch (current.Kind)
                    {
                        case DisabledTextStartPartKind:
                            _disabledTextDepth++;
                            break;
                        case DisabledTextEndPartKind:
                            _disabledTextDepth--;
                            break;
                        case >= 0 when _disabledTextDepth == 0:
                            _hasToken = true;
                            break;
                    }
                }
                else
                {
                    Dispose();
                }
            }

            part = _parts[index];
            return true;
        }

        public void Dispose()
        {
            _enumerator?.Dispose();
            _enumerator = null;
        }
    }
}
