using System.Runtime.CompilerServices;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseBannedSyntaxAnalyzer : DiagnosticAnalyzer
{
    // An entry can set its own severity, and a descriptor carries a single severity, so there is one descriptor per
    // severity. The warning one is first, so it is the one the default severity of the rule uses.
    private static readonly DiagnosticDescriptor Rule = CreateRule(DiagnosticSeverity.Warning);
    private static readonly DiagnosticDescriptor HiddenRule = CreateRule(DiagnosticSeverity.Hidden);
    private static readonly DiagnosticDescriptor InfoRule = CreateRule(DiagnosticSeverity.Info);
    private static readonly DiagnosticDescriptor ErrorRule = CreateRule(DiagnosticSeverity.Error);

    private static DiagnosticDescriptor CreateRule(DiagnosticSeverity severity) => new(
        RuleIdentifiers.DoNotUseBannedSyntax,
        title: "Do not use banned syntax",
        messageFormat: "The syntax '{0}' is banned{1}",
        RuleCategories.Design,
        severity,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBannedSyntax));

    private static readonly DiagnosticDescriptor InvalidEntryRule = new(
        RuleIdentifiers.InvalidBannedSyntaxEntry,
        title: "The banned syntax entry is not valid",
        messageFormat: "The query '{0}' is not valid: {1}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.InvalidBannedSyntaxEntry));

    private const string FileName = "BannedSyntaxes.txt";
    private const string FileNamePrefix = "BannedSyntaxes.";
    private const string FileNameExtension = ".txt";

    // The files are parsed by the compilation start action and by the additional file action, so the result is shared
    private static readonly ConditionalWeakTable<SourceText, BannedSyntaxFile> ParsedFiles = new();

    // The queries come from the files of the analyzed projects, and an editor provides a new content at every keystroke
    private static readonly BoundedCache<string, (XPathExpression? Expression, bool RequiresSemanticModel, string? ErrorMessage)> QueryCache = new(capacity: 128);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, HiddenRule, InfoRule, ErrorRule, InvalidEntryRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(context =>
        {
            // The rule is enabled by default, so it must not cost anything to the projects without a banned syntax file
            if (!HasBannedSyntaxFile(context.Options.AdditionalFiles))
                return;

            context.RegisterAdditionalFileAction(AnalyzeAdditionalFile);

            var configuration = BannedSyntaxConfiguration.Create(context.Options.AdditionalFiles, context.CancellationToken);
            if (configuration is null)
                return;

            // Getting the semantic model of a tree is not free, so it is only requested when a query needs it
            if (configuration.RequiresSemanticModel)
            {
                context.RegisterSemanticModelAction(configuration.AnalyzeSemanticModel);
            }
            else
            {
                context.RegisterSyntaxTreeAction(configuration.AnalyzeTree);
            }
        });
    }

    private static bool HasBannedSyntaxFile(ImmutableArray<AdditionalText> additionalFiles)
    {
        foreach (var additionalFile in additionalFiles)
        {
            if (IsBannedSyntaxFile(additionalFile.Path))
                return true;
        }

        return false;
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

    private static (XPathExpression? Expression, bool RequiresSemanticModel, string? ErrorMessage) GetQuery(string query)
    {
        return QueryCache.GetOrAdd(query, static query =>
        {
            try
            {
                // The resolver defines the 'semantic' prefix. An undefined prefix throws when the query is compiled.
                var expression = XPathExpression.Compile(query, SyntaxNodeXPathNavigator.NamespaceResolver);
                if (expression.ReturnType is not XPathResultType.NodeSet)
                    return (null, false, "The query must return a node-set");

                var (requiresSemanticModel, errorMessage) = ScanSemanticNames(query);
                if (errorMessage is not null)
                    return (null, false, errorMessage);

                // Some errors, such as an unknown function, are only detected when the query is evaluated.
                // The navigator has no semantic model, so the semantic attributes are simply not exposed.
                var navigator = new SyntaxNodeXPathNavigator(SyntaxFactory.CompilationUnit(), CancellationToken.None);
                foreach (var _ in navigator.Select(expression.Clone()))
                {
                }

                return (expression, requiresSemanticModel, null);
            }
            catch (XPathException ex)
            {
                return (null, false, ex.Message);
            }
        });
    }

    // A query using an attribute of the 'semantic' namespace needs the semantic model. An unknown name would silently
    // match nothing, so it is reported instead.
    private static (bool RequiresSemanticModel, string? ErrorMessage) ScanSemanticNames(string query)
    {
        var requiresSemanticModel = false;
        var quote = '\0';
        for (var i = 0; i < query.Length; i++)
        {
            var c = query[i];
            if (quote is not '\0')
            {
                if (c == quote)
                {
                    quote = '\0';
                }

                continue;
            }

            if (c is '\'' or '"')
            {
                quote = c;
                continue;
            }

            if (!IsNameStart(c))
                continue;

            var start = i;
            while (i + 1 < query.Length && IsNamePart(query[i + 1]))
            {
                i++;
            }

            var end = i + 1;

            // 'semantic::' is an axis, not a namespace prefix
            if (end >= query.Length || query[end] is not ':' || (end + 1 < query.Length && query[end + 1] is ':'))
                continue;

            if (!string.Equals(query.Substring(start, end - start), SyntaxNodeXPathNavigator.SemanticPrefix, StringComparison.Ordinal))
                continue;

            requiresSemanticModel = true;

            var nameStart = end + 1;
            var nameEnd = nameStart;
            while (nameEnd < query.Length && (nameEnd == nameStart ? IsNameStart(query[nameEnd]) : IsNamePart(query[nameEnd])))
            {
                nameEnd++;
            }

            var name = query.Substring(nameStart, nameEnd - nameStart);
            if (!SyntaxNodeXPathNavigator.IsSemanticName(name))
                return (true, $"'{name}' is not a valid semantic attribute");

            i = nameEnd - 1;
        }

        return (requiresSemanticModel, null);
    }

    private static bool IsNameStart(char c) => char.IsLetter(c) || c is '_';

    private static bool IsNamePart(char c) => char.IsLetterOrDigit(c) || c is '_' or '-' or '.';

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

    // The names are the ones of the 'dotnet_diagnostic.<id>.severity' option of the .editorconfig files. 'none' is
    // valid and means the entry reports nothing, which turns off an entry coming from a shared file.
    private static (bool IsValid, DiagnosticSeverity? Severity) ParseSeverity(string value) => value.ToUpperInvariant() switch
    {
        "NONE" => (true, null),
        "SILENT" or "HIDDEN" => (true, DiagnosticSeverity.Hidden),
        "SUGGESTION" or "INFO" => (true, DiagnosticSeverity.Info),
        "WARNING" => (true, DiagnosticSeverity.Warning),
        "ERROR" => (true, DiagnosticSeverity.Error),
        _ => (false, null),
    };

    private static DiagnosticDescriptor GetDescriptor(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Hidden => HiddenRule,
        DiagnosticSeverity.Info => InfoRule,
        DiagnosticSeverity.Error => ErrorRule,
        _ => Rule,
    };

    private sealed record BannedSyntaxEntry(TextSpan Span, string Query, string? Message, SyntaxKind? Kind, XPathExpression? Expression, bool RequiresSemanticModel, DiagnosticDescriptor? Descriptor, string? ErrorMessage)
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
                string? severity = null;
                string? message = null;
                if (separatorIndex >= 0)
                {
                    var remainder = content.Substring(separatorIndex + 1);

                    // The line is 'query;severity;message' when the field between the two separators is a single word,
                    // so the message of a 'query;message' line can still contain a ';'
                    var severityIndex = remainder.IndexOf(";", StringComparison.Ordinal);
                    if (severityIndex >= 0 && IsSeverityField(remainder, severityIndex))
                    {
                        severity = remainder.Substring(0, severityIndex).Trim();
                        message = remainder.Substring(severityIndex + 1).Trim();
                    }
                    else
                    {
                        message = remainder.Trim();
                    }
                }

                var span = TextSpan.FromBounds(line.Start + start, line.End);
                entries.Add(CreateEntry(span, query, severity, message));
            }

            return new BannedSyntaxFile(text, entries.ToImmutable());
        }

        private static BannedSyntaxEntry CreateEntry(TextSpan span, string query, string? severity, string? message)
        {
            // A 'none' severity has no descriptor, so the entry is still validated but reports nothing
            DiagnosticDescriptor? descriptor = Rule;
            if (severity is not null)
            {
                var (isValid, parsedSeverity) = ParseSeverity(severity);
                if (!isValid)
                    return new BannedSyntaxEntry(span, query, message, Kind: null, Expression: null, RequiresSemanticModel: false, Descriptor: null, ErrorMessage: $"'{severity}' is not a valid severity");

                descriptor = parsedSeverity is { } value ? GetDescriptor(value) : null;
            }

            // A kind name, or "//" followed by a kind name, selects the nodes of this kind, which is faster to find
            // without evaluating a query. A single name is not a useful XPath query, as it selects the root node only
            // when it is of this kind, so it is always interpreted as a kind.
            var kindStart = query.StartsWith("//", StringComparison.Ordinal) ? 2 : 0;
            if (IsKindName(query, kindStart))
            {
                var name = query.Substring(kindStart);
                if (Enum.TryParse<SyntaxKind>(name, ignoreCase: false, out var kind) && kind is not SyntaxKind.None && Enum.IsDefined(typeof(SyntaxKind), kind))
                    return new BannedSyntaxEntry(span, query, message, kind, Expression: null, RequiresSemanticModel: false, descriptor, ErrorMessage: null);

                return new BannedSyntaxEntry(span, query, message, Kind: null, Expression: null, RequiresSemanticModel: false, Descriptor: null, ErrorMessage: $"'{name}' is not a member of SyntaxKind");
            }

            var (expression, requiresSemanticModel, errorMessage) = GetQuery(query);
            return new BannedSyntaxEntry(span, query, message, Kind: null, expression, requiresSemanticModel, errorMessage is null ? descriptor : null, errorMessage);
        }

        // The field is a severity when it is a single word, such as 'warning'
        private static bool IsSeverityField(string value, int end)
        {
            var start = 0;
            while (start < end && char.IsWhiteSpace(value[start]))
            {
                start++;
            }

            while (end > start && char.IsWhiteSpace(value[end - 1]))
            {
                end--;
            }

            if (start == end)
                return false;

            for (var i = start; i < end; i++)
            {
                if (!char.IsLetter(value[i]))
                    return false;
            }

            return true;
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
        private readonly List<BannedSyntaxEntry> _semanticQueries;

        private BannedSyntaxConfiguration(Dictionary<SyntaxKind, List<BannedSyntaxEntry>> kinds, List<BannedSyntaxEntry> queries, List<BannedSyntaxEntry> semanticQueries)
        {
            _kinds = kinds;
            _queries = queries;
            _semanticQueries = semanticQueries;
        }

        public bool RequiresSemanticModel => _semanticQueries.Count > 0;

        public static BannedSyntaxConfiguration? Create(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
        {
            var kinds = new Dictionary<SyntaxKind, List<BannedSyntaxEntry>>();
            var queries = new List<BannedSyntaxEntry>();
            var semanticQueries = new List<BannedSyntaxEntry>();
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
                    // The entries that are not valid and the entries whose severity is 'none' report nothing
                    if (entry.Descriptor is null)
                        continue;

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
                        (entry.RequiresSemanticModel ? semanticQueries : queries).Add(entry);
                    }
                }
            }

            if (kinds.Count is 0 && queries.Count is 0 && semanticQueries.Count is 0)
                return null;

            return new BannedSyntaxConfiguration(kinds, queries, semanticQueries);
        }

        public void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;
            Analyze(tree.GetRoot(context.CancellationToken), semanticModel: null,
                (span, descriptor, name, message) => context.ReportDiagnostic(descriptor, Location.Create(tree, span), name, message), context.CancellationToken);
        }

        public void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var tree = semanticModel.SyntaxTree;
            Analyze(tree.GetRoot(context.CancellationToken), semanticModel,
                (span, descriptor, name, message) => context.ReportDiagnostic(descriptor, Location.Create(tree, span), name, message), context.CancellationToken);
        }

        private void Analyze(SyntaxNode root, SemanticModel? semanticModel, Action<TextSpan, DiagnosticDescriptor, string, string> report, CancellationToken cancellationToken)
        {
            // The same syntax can be banned by several entries, possibly from several files
            var reported = new HashSet<(TextSpan Span, DiagnosticDescriptor Descriptor, string Name, string Message)>();
            if (_kinds.Count > 0)
            {
                foreach (var node in root.DescendantNodesAndSelf())
                {
                    var kind = node.Kind();
                    if (_kinds.TryGetValue(kind, out var entries))
                    {
                        foreach (var entry in entries)
                        {
                            Report(reported, report, node.Span, kind.ToString(), entry);
                        }
                    }
                }
            }

            // The entries that do not use the semantic model are evaluated on a navigator that does not expose the
            // semantic attributes, so they do not pay for them
            if (_queries.Count > 0)
            {
                Evaluate(_queries, new SyntaxNodeXPathNavigator(root, cancellationToken), reported, report);
            }

            if (_semanticQueries.Count > 0 && semanticModel is not null)
            {
                Evaluate(_semanticQueries, new SyntaxNodeXPathNavigator(root, semanticModel, cancellationToken), reported, report);
            }
        }

        private static void Evaluate(List<BannedSyntaxEntry> entries, SyntaxNodeXPathNavigator navigator, HashSet<(TextSpan Span, DiagnosticDescriptor Descriptor, string Name, string Message)> reported, Action<TextSpan, DiagnosticDescriptor, string, string> report)
        {
            foreach (var entry in entries)
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

                        Report(reported, report, match.Span, name, entry);
                    }
                }
                catch (XPathException)
                {
                    // The query is validated when the file is parsed, so this only happens for an error that depends on the tree
                }
            }
        }

        private static void Report(HashSet<(TextSpan Span, DiagnosticDescriptor Descriptor, string Name, string Message)> reported, Action<TextSpan, DiagnosticDescriptor, string, string> report, TextSpan span, string name, BannedSyntaxEntry entry)
        {
            var descriptor = entry.Descriptor!;
            var message = entry.FormattedMessage;
            if (reported.Add((span, descriptor, name, message)))
            {
                report(span, descriptor, name, message);
            }
        }
    }
}
