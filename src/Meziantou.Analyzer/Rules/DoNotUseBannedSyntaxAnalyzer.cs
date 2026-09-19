using System.Runtime.CompilerServices;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseBannedSyntaxAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseBannedSyntax,
        title: "Do not use banned syntax",
        messageFormat: "The syntax '{0}' is banned{1}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBannedSyntax));

    // Uses the same id as the rule, so the invalid entries are reported only to the projects that enable the rule
    private static readonly DiagnosticDescriptor InvalidEntryRule = new(
        RuleIdentifiers.DoNotUseBannedSyntax,
        title: "Do not use banned syntax",
        messageFormat: "The query '{0}' is not valid: {1}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBannedSyntax));

    private const string FileName = "BannedSyntaxes.txt";
    private const string FileNamePrefix = "BannedSyntaxes.";
    private const string FileNameExtension = ".txt";

    // The files are parsed by the compilation start action and by the additional file action, so the result is shared
    private static readonly ConditionalWeakTable<SourceText, BannedSyntaxFile> ParsedFiles = new();

    // The queries come from the files of the analyzed projects, and an editor provides a new content at every keystroke
    private static readonly BoundedCache<string, (XPathExpression? Expression, string? ErrorMessage)> QueryCache = new(capacity: 128);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, InvalidEntryRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            var configuration = BannedSyntaxConfiguration.Create(context.Options.AdditionalFiles, context.CancellationToken);
            if (configuration is not null)
            {
                context.RegisterSyntaxTreeAction(configuration.AnalyzeTree);
            }
        });

        context.RegisterAdditionalFileAction(AnalyzeAdditionalFile);
    }

    private static void AnalyzeAdditionalFile(AdditionalFileAnalysisContext context)
    {
        if (!IsBannedSyntaxFile(context.AdditionalFile.Path))
            return;

        // The package adds the closest BannedSyntaxes.txt file to the project, which can also be added by the project itself
        if (!IsFirstOccurrence(context.Options.AdditionalFiles, context.AdditionalFile))
            return;

        var file = GetParsedFile(context.AdditionalFile, context.CancellationToken);
        if (file is null)
            return;

        foreach (var entry in file.Entries)
        {
            if (entry.ErrorMessage is not null)
            {
                var location = Location.Create(context.AdditionalFile.Path, entry.Span, file.Text.Lines.GetLinePositionSpan(entry.Span));
                context.ReportDiagnostic(InvalidEntryRule, location, entry.Query, entry.ErrorMessage);
            }
        }
    }

    private static bool IsFirstOccurrence(ImmutableArray<AdditionalText> additionalFiles, AdditionalText additionalFile)
    {
        foreach (var file in additionalFiles)
        {
            if (string.Equals(file.Path, additionalFile.Path, StringComparison.Ordinal))
                return ReferenceEquals(file, additionalFile);
        }

        return true;
    }

    private static bool IsBannedSyntaxFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return string.Equals(fileName, FileName, StringComparison.OrdinalIgnoreCase)
            || (fileName.StartsWith(FileNamePrefix, StringComparison.OrdinalIgnoreCase) && fileName.EndsWith(FileNameExtension, StringComparison.OrdinalIgnoreCase));
    }

    private static BannedSyntaxFile? GetParsedFile(AdditionalText additionalText, CancellationToken cancellationToken)
    {
        var text = additionalText.GetText(cancellationToken);
        if (text is null)
            return null;

        return ParsedFiles.GetValue(text, BannedSyntaxFile.Parse);
    }

    private static (XPathExpression? Expression, string? ErrorMessage) GetQuery(string query)
    {
        return QueryCache.GetOrAdd(query, static query =>
        {
            try
            {
                var expression = XPathExpression.Compile(query);
                if (expression.ReturnType is not XPathResultType.NodeSet)
                    return (null, "The query must return a node-set");

                // Some errors, such as an unknown function, are only detected when the query is evaluated
                var navigator = new SyntaxNodeXPathNavigator(SyntaxFactory.CompilationUnit(), CancellationToken.None);
                foreach (var _ in navigator.Select(expression.Clone()))
                {
                }

                return (expression, null);
            }
            catch (XPathException ex)
            {
                return (null, ex.Message);
            }
        });
    }

    private static bool IsKindName(string value, int start)
    {
        if (start >= value.Length || !char.IsLetter(value[start]))
            return false;

        for (var i = start + 1; i < value.Length; i++)
        {
            if (!char.IsLetterOrDigit(value[i]))
                return false;
        }

        return true;
    }

    private sealed record BannedSyntaxEntry(TextSpan Span, string Query, string? Message, SyntaxKind? Kind, XPathExpression? Expression, string? ErrorMessage)
    {
        // The argument of the message, so the message ends with the custom message when there is one
        public string FormattedMessage => string.IsNullOrEmpty(Message) ? "" : ": " + Message;
    }

    private sealed class BannedSyntaxFile(SourceText text, ImmutableArray<BannedSyntaxEntry> entries)
    {
        public SourceText Text { get; } = text;
        public ImmutableArray<BannedSyntaxEntry> Entries { get; } = entries;

        public static BannedSyntaxFile Parse(SourceText text)
        {
            var entries = ImmutableArray.CreateBuilder<BannedSyntaxEntry>();
            foreach (var line in text.Lines)
            {
                var content = line.ToString();
                var start = 0;
                while (start < content.Length && char.IsWhiteSpace(content[start]))
                {
                    start++;
                }

                // Lines starting with '#' are comments. '#' is not used by XPath, whereas '//' starts most queries.
                if (start == content.Length || content[start] is '#')
                    continue;

                var separatorIndex = IndexOfSeparator(content, start);
                var query = (separatorIndex < 0 ? content.Substring(start) : content.Substring(start, separatorIndex - start)).TrimEnd();
                var message = separatorIndex < 0 ? null : content.Substring(separatorIndex + 1).Trim();
                var span = TextSpan.FromBounds(line.Start + start, line.End);
                entries.Add(CreateEntry(span, query, message));
            }

            return new BannedSyntaxFile(text, entries.ToImmutable());
        }

        private static BannedSyntaxEntry CreateEntry(TextSpan span, string query, string? message)
        {
            // A kind name, or "//" followed by a kind name, selects the nodes of this kind, which is faster to find
            // without evaluating a query. A single name is not a useful XPath query, as it selects the root node only
            // when it is of this kind, so it is always interpreted as a kind.
            var kindStart = query.StartsWith("//", StringComparison.Ordinal) ? 2 : 0;
            if (IsKindName(query, kindStart))
            {
                var name = query.Substring(kindStart);
                if (Enum.TryParse<SyntaxKind>(name, ignoreCase: false, out var kind) && kind is not SyntaxKind.None && Enum.IsDefined(typeof(SyntaxKind), kind))
                    return new BannedSyntaxEntry(span, query, message, kind, Expression: null, ErrorMessage: null);

                return new BannedSyntaxEntry(span, query, message, Kind: null, Expression: null, ErrorMessage: $"'{name}' is not a member of SyntaxKind");
            }

            var (expression, errorMessage) = GetQuery(query);
            return new BannedSyntaxEntry(span, query, message, Kind: null, expression, errorMessage);
        }

        // The query and the message are separated by the first ';' that is not in an XPath string literal
        private static int IndexOfSeparator(string value, int start)
        {
            var quote = '\0';
            for (var i = start; i < value.Length; i++)
            {
                var c = value[i];
                if (quote is not '\0')
                {
                    if (c == quote)
                    {
                        quote = '\0';
                    }
                }
                else if (c is '\'' or '"')
                {
                    quote = c;
                }
                else if (c is ';')
                {
                    return i;
                }
            }

            return -1;
        }
    }

    private sealed class BannedSyntaxConfiguration
    {
        private readonly Dictionary<SyntaxKind, List<BannedSyntaxEntry>> _kinds;
        private readonly List<BannedSyntaxEntry> _queries;

        private BannedSyntaxConfiguration(Dictionary<SyntaxKind, List<BannedSyntaxEntry>> kinds, List<BannedSyntaxEntry> queries)
        {
            _kinds = kinds;
            _queries = queries;
        }

        public static BannedSyntaxConfiguration? Create(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
        {
            var kinds = new Dictionary<SyntaxKind, List<BannedSyntaxEntry>>();
            var queries = new List<BannedSyntaxEntry>();
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (var additionalFile in additionalFiles)
            {
                if (!IsBannedSyntaxFile(additionalFile.Path) || !paths.Add(additionalFile.Path))
                    continue;

                var file = GetParsedFile(additionalFile, cancellationToken);
                if (file is null)
                    continue;

                foreach (var entry in file.Entries)
                {
                    if (entry.Kind is { } kind)
                    {
                        if (!kinds.TryGetValue(kind, out var entries))
                        {
                            entries = [];
                            kinds.Add(kind, entries);
                        }

                        entries.Add(entry);
                    }
                    else if (entry.Expression is not null)
                    {
                        queries.Add(entry);
                    }
                }
            }

            if (kinds.Count is 0 && queries.Count is 0)
                return null;

            return new BannedSyntaxConfiguration(kinds, queries);
        }

        public void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot(context.CancellationToken);

            // The same syntax can be banned by several entries, possibly from several files
            var reported = new HashSet<(TextSpan Span, string Name, string Message)>();
            if (_kinds.Count > 0)
            {
                foreach (var node in root.DescendantNodesAndSelf())
                {
                    var kind = node.Kind();
                    if (_kinds.TryGetValue(kind, out var entries))
                    {
                        foreach (var entry in entries)
                        {
                            Report(context, reported, node.Span, kind.ToString(), entry);
                        }
                    }
                }
            }

            if (_queries.Count is 0)
                return;

            var navigator = new SyntaxNodeXPathNavigator(root, context.CancellationToken);
            foreach (var entry in _queries)
            {
                try
                {
                    // A compiled expression is not thread-safe, so each evaluation uses its own copy
                    foreach (SyntaxNodeXPathNavigator match in navigator.Select(entry.Expression!.Clone()))
                    {
                        if (match.Node is null)
                            continue;

                        var name = match.Node.Kind().ToString();
                        if (match.AttributeName is { } attributeName)
                        {
                            name += "/@" + attributeName;
                        }

                        Report(context, reported, match.Span, name, entry);
                    }
                }
                catch (XPathException)
                {
                    // The query is validated when the file is parsed, so this only happens for an error that depends on the tree
                }
            }
        }

        private static void Report(SyntaxTreeAnalysisContext context, HashSet<(TextSpan Span, string Name, string Message)> reported, TextSpan span, string name, BannedSyntaxEntry entry)
        {
            var message = entry.FormattedMessage;
            if (reported.Add((span, name, message)))
            {
                context.ReportDiagnostic(Rule, Location.Create(context.Tree, span), name, message);
            }
        }
    }
}
