using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseNonAsciiCharactersAnalyzer : DiagnosticAnalyzer
{
    private const int ReadBufferSize = 4096;

    private static readonly ConfigurationDefinition<string> AdditionalAllowedRangesConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".additional_allowed_ranges", defaultValue: "");
    private static readonly ConfigurationDefinition<bool> AllowEmojiConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".allow_emoji", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> AllowVisibleCharactersConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".allow_visible_characters", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> SkipRawStringsConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".skip_raw_strings", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> SkipStringsConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".skip_strings", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> SkipVerbatimStringsConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".skip_verbatim_strings", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> SkipCharsConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".skip_chars", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> SkipCommentsConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".skip_comments", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> SkipDocumentationCommentsConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".skip_documentation_comments", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> SkipIdentifiersConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".skip_identifiers", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> SkipIndentationConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".skip_indentation", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> AlwaysReportInvisibleCharactersConfiguration = new(RuleIdentifiers.DoNotUseNonAsciiCharacters + ".always_report_invisible_characters", defaultValue: true);

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseNonAsciiCharacters,
        title: "Do not use non-ASCII characters",
        messageFormat: "The {0} contains disallowed characters: {1}",
        RuleCategories.Style,
        DiagnosticSeverity.Info,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseNonAsciiCharacters));

    // The code points of the Unicode Extended_Pictographic property (emoji-data.txt), and the regional indicators used by the flags
    private static readonly (int Start, int End)[] EmojiRanges =
    [
        (0x00A9, 0x00A9), (0x00AE, 0x00AE), (0x203C, 0x203C), (0x2049, 0x2049), (0x2122, 0x2122), (0x2139, 0x2139),
        (0x2194, 0x2199), (0x21A9, 0x21AA), (0x231A, 0x231B), (0x2328, 0x2328), (0x2388, 0x2388), (0x23CF, 0x23CF),
        (0x23E9, 0x23F3), (0x23F8, 0x23FA), (0x24C2, 0x24C2), (0x25AA, 0x25AB), (0x25B6, 0x25B6), (0x25C0, 0x25C0),
        (0x25FB, 0x25FE), (0x2600, 0x2605), (0x2607, 0x2612), (0x2614, 0x2685), (0x2690, 0x2705), (0x2708, 0x2712),
        (0x2714, 0x2714), (0x2716, 0x2716), (0x271D, 0x271D), (0x2721, 0x2721), (0x2728, 0x2728), (0x2733, 0x2734),
        (0x2744, 0x2744), (0x2747, 0x2747), (0x274C, 0x274C), (0x274E, 0x274E), (0x2753, 0x2755), (0x2757, 0x2757),
        (0x2763, 0x2767), (0x2795, 0x2797), (0x27A1, 0x27A1), (0x27B0, 0x27B0), (0x27BF, 0x27BF), (0x2934, 0x2935),
        (0x2B05, 0x2B07), (0x2B1B, 0x2B1C), (0x2B50, 0x2B50), (0x2B55, 0x2B55), (0x3030, 0x3030), (0x303D, 0x303D),
        (0x3297, 0x3297), (0x3299, 0x3299), (0x1F000, 0x1F0FF), (0x1F10D, 0x1F10F), (0x1F12F, 0x1F12F), (0x1F16C, 0x1F171),
        (0x1F17E, 0x1F17F), (0x1F18E, 0x1F18E), (0x1F191, 0x1F19A), (0x1F1AD, 0x1F1FF), (0x1F201, 0x1F20F), (0x1F21A, 0x1F21A),
        (0x1F22F, 0x1F22F), (0x1F232, 0x1F23A), (0x1F23C, 0x1F23F), (0x1F249, 0x1F3FA), (0x1F400, 0x1F53D), (0x1F546, 0x1F64F),
        (0x1F680, 0x1F6FF), (0x1F774, 0x1F77F), (0x1F7D5, 0x1F7FF), (0x1F80C, 0x1F80F), (0x1F848, 0x1F84F), (0x1F85A, 0x1F85F),
        (0x1F888, 0x1F88F), (0x1F8AE, 0x1F8FF), (0x1F90C, 0x1F93A), (0x1F93C, 0x1F945), (0x1F947, 0x1FAFF), (0x1FC00, 0x1FFFD),
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var allowedRangesCache = new ConcurrentDictionary<string, (int Start, int End)[]>(StringComparer.Ordinal);
            context.RegisterSyntaxTreeAction(context => AnalyzeTree(context, allowedRangesCache));
        });
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context, ConcurrentDictionary<string, (int Start, int End)[]> allowedRangesCache)
    {
        // Most files only contain ASCII characters, and reading the text is much faster than enumerating the tokens and the trivia
        var sourceText = context.Tree.GetText(context.CancellationToken);
        if (!ContainsCharacterOutsideBaseSet(sourceText))
            return;

        var options = new Options(context.Options, context.Tree, allowedRangesCache);
        var analyzer = new TreeAnalyzer(context, sourceText.ToString(), options);
        analyzer.Analyze();
    }

    private static bool ContainsCharacterOutsideBaseSet(SourceText text)
    {
        var buffer = ArrayPool<char>.Shared.Rent(ReadBufferSize);
        try
        {
            for (var start = 0; start < text.Length; start += ReadBufferSize)
            {
                var length = Math.Min(ReadBufferSize, text.Length - start);
                text.CopyTo(start, buffer, 0, length);
                for (var i = 0; i < length; i++)
                {
                    if (!IsInBaseSet(buffer[i]))
                        return true;
                }
            }

            return false;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }

    private static bool IsInBaseSet(char c) => c is (>= ' ' and <= '~') or '\t' or '\r' or '\n';

    private static bool IsEmoji(int codePoint) => IsInRanges(EmojiRanges, codePoint);

    private static bool IsRegionalIndicator(int codePoint) => codePoint is >= 0x1F1E6 and <= 0x1F1FF;

    private static bool IsInRanges((int Start, int End)[] ranges, int codePoint)
    {
        var low = 0;
        var high = ranges.Length - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var range = ranges[middle];
            if (codePoint < range.Start)
            {
                high = middle - 1;
            }
            else if (codePoint > range.End)
            {
                low = middle + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInvisible(string text, int index, int codePoint)
    {
        if (codePoint is 0x115F or 0x1160 or 0x3164 or 0xFFA0 or 0x2800 or (>= 0xFE00 and <= 0xFE0F) or (>= 0xE0100 and <= 0xE01EF))
            return true;

        return CharUnicodeInfo.GetUnicodeCategory(text, index) switch
        {
            UnicodeCategory.Control => true,
            UnicodeCategory.Format => true,
            UnicodeCategory.SpaceSeparator => codePoint is not ' ',
            UnicodeCategory.LineSeparator => true,
            UnicodeCategory.ParagraphSeparator => true,
            UnicodeCategory.Surrogate => true,
            UnicodeCategory.PrivateUse => true,
            UnicodeCategory.OtherNotAssigned => true,
            _ => false,
        };
    }

    private static (int Start, int End)[] ParseAllowedRanges(string value)
    {
        var result = new List<(int Start, int End)>();
        foreach (var entry in value.Split(','))
        {
            var separatorIndex = entry.IndexOf('-', StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                if (TryParseCodePoint(entry, out var codePoint))
                {
                    result.Add((codePoint, codePoint));
                }
            }
            else if (TryParseCodePoint(entry.Substring(0, separatorIndex), out var start) && TryParseCodePoint(entry.Substring(separatorIndex + 1), out var end) && start <= end)
            {
                result.Add((start, end));
            }
        }

        // IsInRanges uses a binary search, so the ranges must be sorted and must not overlap
        result.Sort((a, b) => a.Start.CompareTo(b.Start));
        var merged = new List<(int Start, int End)>(result.Count);
        foreach (var range in result)
        {
            if (merged.Count > 0 && range.Start <= merged[^1].End + 1)
            {
                merged[^1] = (merged[^1].Start, Math.Max(merged[^1].End, range.End));
            }
            else
            {
                merged.Add(range);
            }
        }

        return [.. merged];

        static bool TryParseCodePoint(string value, out int codePoint)
        {
            value = value.Trim();
            if (value.StartsWith("U+", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(2);
            }

            return int.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out codePoint) && codePoint <= 0x10FFFF;
        }
    }

    private enum ElementKind
    {
        Code,
        String,
        VerbatimString,
        RawString,
        Character,
        Identifier,
        Comment,
        DocumentationComment,
        PreprocessorDirective,
        DisabledCode,
        Whitespace,
    }

    private static string GetElementName(ElementKind kind) => kind switch
    {
        ElementKind.String => "string literal",
        ElementKind.VerbatimString => "verbatim string literal",
        ElementKind.RawString => "raw string literal",
        ElementKind.Character => "character literal",
        ElementKind.Identifier => "identifier",
        ElementKind.Comment => "comment",
        ElementKind.DocumentationComment => "documentation comment",
        ElementKind.PreprocessorDirective => "preprocessor directive",
        ElementKind.DisabledCode => "disabled code",
        ElementKind.Whitespace => "whitespace",
        _ => "code",
    };

    private static bool IsLiteral(ElementKind kind) => kind is ElementKind.String or ElementKind.VerbatimString or ElementKind.RawString or ElementKind.Character;

    private sealed class Options
    {
        public Options(AnalyzerOptions options, SyntaxTree tree, ConcurrentDictionary<string, (int Start, int End)[]> allowedRangesCache)
        {
            AllowedRanges = allowedRangesCache.GetOrAdd(options.GetConfigurationValue(tree, AdditionalAllowedRangesConfiguration), ParseAllowedRanges);
            AllowEmoji = options.GetConfigurationValue(tree, AllowEmojiConfiguration);
            AllowVisibleCharacters = options.GetConfigurationValue(tree, AllowVisibleCharactersConfiguration);
            SkipRawStrings = options.GetConfigurationValue(tree, SkipRawStringsConfiguration);
            SkipStrings = options.GetConfigurationValue(tree, SkipStringsConfiguration);
            SkipVerbatimStrings = options.GetConfigurationValue(tree, SkipVerbatimStringsConfiguration);
            SkipChars = options.GetConfigurationValue(tree, SkipCharsConfiguration);
            SkipComments = options.GetConfigurationValue(tree, SkipCommentsConfiguration);
            SkipDocumentationComments = options.GetConfigurationValue(tree, SkipDocumentationCommentsConfiguration);
            SkipIdentifiers = options.GetConfigurationValue(tree, SkipIdentifiersConfiguration);
            SkipIndentation = options.GetConfigurationValue(tree, SkipIndentationConfiguration);
            AlwaysReportInvisibleCharacters = options.GetConfigurationValue(tree, AlwaysReportInvisibleCharactersConfiguration);
        }

        public (int Start, int End)[] AllowedRanges { get; }
        public bool AllowEmoji { get; }
        public bool AllowVisibleCharacters { get; }
        public bool SkipRawStrings { get; }
        public bool SkipStrings { get; }
        public bool SkipVerbatimStrings { get; }
        public bool SkipChars { get; }
        public bool SkipComments { get; }
        public bool SkipDocumentationComments { get; }
        public bool SkipIdentifiers { get; }
        public bool SkipIndentation { get; }
        public bool AlwaysReportInvisibleCharacters { get; }

        public bool IsSkipped(ElementKind kind) => kind switch
        {
            ElementKind.RawString => SkipRawStrings,
            ElementKind.String => SkipStrings,
            ElementKind.VerbatimString => SkipVerbatimStrings,
            ElementKind.Character => SkipChars,
            ElementKind.Comment => SkipComments,
            ElementKind.DocumentationComment => SkipDocumentationComments,
            ElementKind.Identifier => SkipIdentifiers,
            _ => false,
        };
    }

    private sealed class TreeAnalyzer(SyntaxTreeAnalysisContext context, string text, Options options)
    {
        private readonly SyntaxNode _root = context.Tree.GetRoot(context.CancellationToken);
        private readonly StringBuilder _runCodePoints = new();

        private TextSpan _elementSpan;
        private ElementKind _elementKind;
        private bool _hasElement;

        private int _runStart = -1;
        private int _runEnd;
        private ElementKind _runElementKind;

        public void Analyze()
        {
            var isInIndentation = true;
            var emojiSequenceEnd = -1;
            var index = 0;
            while (index < text.Length)
            {
                var c = text[index];
                if (IsInBaseSet(c))
                {
                    if (c is '\r' or '\n')
                    {
                        isInIndentation = true;
                    }
                    else if (c is not (' ' or '\t'))
                    {
                        isInIndentation = false;
                    }

                    if (options.AllowEmoji && c is (>= '0' and <= '9') or '#' or '*')
                    {
                        emojiSequenceEnd = Math.Max(emojiSequenceEnd, GetKeycapSequenceEnd(index + 1));
                    }

                    FlushRun();
                    index++;
                    continue;
                }

                var length = char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]) ? 2 : 1;
                var codePoint = length is 2 ? char.ConvertToUtf32(c, text[index + 1]) : c;

                var isWhitespace = char.IsWhiteSpace(c);
                var isIndentation = isInIndentation && isWhitespace;
                if (codePoint is 0x85 or 0x2028 or 0x2029)
                {
                    // These characters are line terminators in C#
                    isInIndentation = true;
                }
                else if (!isWhitespace)
                {
                    isInIndentation = false;
                }

                var isInEmojiSequence = false;
                if (options.AllowEmoji)
                {
                    if (index < emojiSequenceEnd)
                    {
                        isInEmojiSequence = true;
                    }
                    else if (IsEmoji(codePoint))
                    {
                        emojiSequenceEnd = GetEmojiSequenceEnd(index + length);
                        isInEmojiSequence = true;
                    }
                }

                UpdateElement(index);
                if (isIndentation && IsLiteral(_elementKind))
                {
                    isIndentation = false;
                }

                if (ShouldReport(index, codePoint, isIndentation, isInEmojiSequence))
                {
                    AddToRun(index, length, codePoint);
                }
                else
                {
                    FlushRun();
                }

                index += length;
            }

            FlushRun();
        }

        private bool ShouldReport(int index, int codePoint, bool isIndentation, bool isInEmojiSequence)
        {
            if (isInEmojiSequence)
                return false;

            if (isIndentation && options.SkipIndentation)
                return false;

            var isInvisible = IsInvisible(text, index, codePoint);
            if (isInvisible && options.AlwaysReportInvisibleCharacters)
                return true;

            if (options.IsSkipped(_elementKind))
                return false;

            if (IsInRanges(options.AllowedRanges, codePoint))
                return false;

            if (!isInvisible && options.AllowVisibleCharacters)
                return false;

            return true;
        }

        private int GetEmojiSequenceEnd(int index)
        {
            while (index < text.Length && TryGetCodePoint(index, out var codePoint))
            {
                var length = codePoint > 0xFFFF ? 2 : 1;
                if (codePoint is 0xFE0E or 0xFE0F or 0x20E3 or (>= 0x1F3FB and <= 0x1F3FF) or (>= 0xE0020 and <= 0xE007F) || IsRegionalIndicator(codePoint))
                {
                    index += length;
                }
                else if (codePoint is 0x200D && index + 1 < text.Length && TryGetCodePoint(index + 1, out var next) && IsEmoji(next))
                {
                    index += 1 + (next > 0xFFFF ? 2 : 1);
                }
                else
                {
                    break;
                }
            }

            return index;
        }

        private int GetKeycapSequenceEnd(int index)
        {
            if (index < text.Length && text[index] is '\uFE0F')
            {
                index++;
            }

            if (index < text.Length && text[index] is '\u20E3')
                return index + 1;

            return -1;
        }

        private bool TryGetCodePoint(int index, out int codePoint)
        {
            var c = text[index];
            if (char.IsHighSurrogate(c))
            {
                if (index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
                {
                    codePoint = char.ConvertToUtf32(c, text[index + 1]);
                    return true;
                }

                codePoint = 0;
                return false;
            }

            if (char.IsLowSurrogate(c))
            {
                codePoint = 0;
                return false;
            }

            codePoint = c;
            return true;
        }

        private void UpdateElement(int position)
        {
            if (_hasElement && _elementSpan.Contains(position))
                return;

            _hasElement = true;
            var token = _root.FindToken(position, findInsideTrivia: true);
            if (token.Span.Contains(position))
            {
                _elementSpan = token.Span;
                _elementKind = GetTokenKind(token);
                return;
            }

            var trivia = _root.FindTrivia(position, findInsideTrivia: true);
            if (trivia.Span.Contains(position))
            {
                _elementSpan = trivia.Span;
                _elementKind = trivia.Kind() switch
                {
                    SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia => ElementKind.Comment,
                    SyntaxKind.DisabledTextTrivia => ElementKind.DisabledCode,
                    SyntaxKind.PreprocessingMessageTrivia => ElementKind.PreprocessorDirective,
                    _ => ElementKind.Whitespace,
                };
                return;
            }

            _elementSpan = new TextSpan(position, 1);
            _elementKind = ElementKind.Code;
        }

        private static ElementKind GetTokenKind(SyntaxToken token)
        {
            switch (token.Parent?.FirstAncestorOrSelf<StructuredTriviaSyntax>())
            {
                case DocumentationCommentTriviaSyntax:
                    return ElementKind.DocumentationComment;
                case DirectiveTriviaSyntax:
                    return ElementKind.PreprocessorDirective;
            }

            switch (token.Kind())
            {
                case SyntaxKind.SingleLineRawStringLiteralToken:
                case SyntaxKind.MultiLineRawStringLiteralToken:
                case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                    return ElementKind.RawString;

                case SyntaxKind.StringLiteralToken:
                case SyntaxKind.Utf8StringLiteralToken:
                    return token.Text is ['@', ..] ? ElementKind.VerbatimString : ElementKind.String;

                case SyntaxKind.CharacterLiteralToken:
                    return ElementKind.Character;

                case SyntaxKind.IdentifierToken:
                    return ElementKind.Identifier;

                case SyntaxKind.InterpolatedStringTextToken:
                    return token.Parent?.FirstAncestorOrSelf<InterpolatedStringExpressionSyntax>()?.StringStartToken.Kind() switch
                    {
                        SyntaxKind.InterpolatedSingleLineRawStringStartToken or SyntaxKind.InterpolatedMultiLineRawStringStartToken => ElementKind.RawString,
                        SyntaxKind.InterpolatedVerbatimStringStartToken => ElementKind.VerbatimString,
                        _ => ElementKind.String,
                    };

                default:
                    return ElementKind.Code;
            }
        }

        private void AddToRun(int index, int length, int codePoint)
        {
            if (_runStart >= 0 && (_runEnd != index || _runElementKind != _elementKind || !_elementSpan.Contains(_runStart)))
            {
                FlushRun();
            }

            if (_runStart < 0)
            {
                _runStart = index;
                _runElementKind = _elementKind;
            }
            else
            {
                _runCodePoints.Append(", ");
            }

            _runEnd = index + length;
            _runCodePoints.Append("U+").Append(codePoint.ToString("X4", CultureInfo.InvariantCulture));
        }

        private void FlushRun()
        {
            if (_runStart < 0)
                return;

            var location = context.Tree.GetLocation(TextSpan.FromBounds(_runStart, _runEnd));
            context.ReportDiagnostic(Rule, location, GetElementName(_runElementKind), _runCodePoints.ToString());
            _runStart = -1;
            _runCodePoints.Clear();
        }
    }
}
