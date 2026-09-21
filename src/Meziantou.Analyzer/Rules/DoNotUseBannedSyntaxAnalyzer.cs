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
    private static readonly BoundedCache<string, (XPathExpression? Expression, BannedSyntaxTarget Target, string? ErrorMessage)> QueryCache = new(capacity: 128);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, InvalidEntryRule);

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

    private static (XPathExpression? Expression, BannedSyntaxTarget Target, string? ErrorMessage) GetQuery(string query)
    {
        return QueryCache.GetOrAdd(query, static query =>
        {
            try
            {
                // The context defines the 'semantic' and 'operation' prefixes and the 'syntax' function. An undefined
                // prefix throws when the query is compiled, and an expression that is not compiled with an XsltContext
                // cannot use a function of its own, whatever the context it is evaluated with.
                var expression = XPathExpression.Compile(query, BannedSyntaxXsltContext.Empty);
                if (expression.ReturnType is not XPathResultType.NodeSet)
                    return (null, BannedSyntaxTarget.Syntax, "The query must return a node-set");

                var (target, errorMessage) = ScanPrefixedNames(query);
                if (errorMessage is not null)
                    return (null, BannedSyntaxTarget.Syntax, errorMessage);

                // Some errors, such as an unknown function, are only detected when the query is evaluated. The empty
                // document has no semantic model and no operation, so those attributes are simply not exposed.
                XPathNavigator navigator = target is BannedSyntaxTarget.Operations
                    ? new OperationXPathNavigator(OperationForest.Empty, CancellationToken.None)
                    : new SyntaxNodeXPathNavigator(SyntaxFactory.CompilationUnit(), CancellationToken.None);

                foreach (var _ in navigator.Select(Prepare(expression, target is BannedSyntaxTarget.Operations ? BannedSyntaxXsltContext.Empty : null)))
                {
                }

                return (expression, target, null);
            }
            catch (XPathException ex)
            {
                return (null, BannedSyntaxTarget.Syntax, ex.Message);
            }
        });
    }

    // A query using an attribute of the 'semantic' namespace needs the semantic model, and a query using an element of
    // the 'operation' namespace is evaluated on the operations. An unknown name would silently match nothing, so it is
    // reported instead.
    private static (BannedSyntaxTarget Target, string? ErrorMessage) ScanPrefixedNames(string query)
    {
        var usesSemanticModel = false;
        var usesOperations = false;
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

            var identifier = query.Substring(start, end - start);

            // The 'syntax' function returns the nodes of the operations, so a query using it is about the operations
            if (IsFunctionCall(query, end) && string.Equals(identifier, BannedSyntaxXsltContext.SyntaxFunctionName, StringComparison.Ordinal))
            {
                usesOperations = true;
                continue;
            }

            var prefix = identifier;
            var isSemantic = string.Equals(prefix, XPathNamespaces.SemanticPrefix, StringComparison.Ordinal);
            var isOperation = string.Equals(prefix, XPathNamespaces.OperationPrefix, StringComparison.Ordinal);

            // The context resolves the prefixes when the query is evaluated, so an undefined prefix selects nothing
            // instead of throwing when the query is compiled
            if (!isSemantic && !isOperation)
                return (BannedSyntaxTarget.Syntax, $"'{prefix}' is not a defined namespace prefix");

            usesSemanticModel |= isSemantic;
            usesOperations |= isOperation;

            var nameStart = end + 1;
            var nameEnd = nameStart;
            while (nameEnd < query.Length && (nameEnd == nameStart ? IsNameStart(query[nameEnd]) : IsNamePart(query[nameEnd])))
            {
                nameEnd++;
            }

            var name = query.Substring(nameStart, nameEnd - nameStart);
            if (isSemantic)
            {
                if (!SyntaxNodeXPathNavigator.IsSemanticName(name))
                    return (BannedSyntaxTarget.SemanticSyntax, $"'{name}' is not a valid semantic attribute");
            }
            else if (name.Length > 0 && !IsOperationKindName(name))
            {
                // 'operation:*' selects every operation, so it has no name to validate
                return (BannedSyntaxTarget.Operations, $"'{name}' is not a kind of operation");
            }

            i = nameEnd - 1;
        }

        // A query is evaluated on a single document, and the operations expose no semantic attribute
        if (usesSemanticModel && usesOperations)
            return (BannedSyntaxTarget.Operations, $"A query cannot use both the '{XPathNamespaces.SemanticPrefix}' and the '{XPathNamespaces.OperationPrefix}' prefixes");

        if (usesOperations)
            return (BannedSyntaxTarget.Operations, null);

        return (usesSemanticModel ? BannedSyntaxTarget.SemanticSyntax : BannedSyntaxTarget.Syntax, null);
    }

    // The name is followed by '(', so it is the name of a function and not the name of an element
    private static bool IsFunctionCall(string query, int index)
    {
        while (index < query.Length && char.IsWhiteSpace(query[index]))
        {
            index++;
        }

        return index < query.Length && query[index] is '(';
    }

    // A compiled expression is not thread-safe, so each evaluation uses its own copy. The context of the copy provides
    // the 'syntax' function of the file that is analyzed.
    private static XPathExpression Prepare(XPathExpression expression, BannedSyntaxXsltContext? context)
    {
        var clone = expression.Clone();
        if (context is not null)
        {
            clone.SetContext(context);
        }

        return clone;
    }

    // The name must be the one the elements use, so an obsolete alias of a kind, such as 'BinaryOperator', is
    // reported instead of silently selecting nothing
    private static bool IsOperationKindName(string name) => OperationXPathNavigator.IsKindName(name);

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
    // valid, and its severity is null, as the entry reports nothing.
    private static (bool IsValid, DiagnosticSeverity? Severity) ParseSeverity(string value) => value.ToUpperInvariant() switch
    {
        "NONE" => (true, null),
        "SILENT" or "HIDDEN" => (true, DiagnosticSeverity.Hidden),
        "SUGGESTION" or "INFO" => (true, DiagnosticSeverity.Info),
        "WARNING" => (true, DiagnosticSeverity.Warning),
        "ERROR" => (true, DiagnosticSeverity.Error),
        _ => (false, null),
    };

    // The document a query is evaluated on. A query uses the syntax tree or the operations, never both.
    private enum BannedSyntaxTarget
    {
        Syntax,
        SemanticSyntax,
        Operations,
    }

    // The severity is null when the entry does not set one, so it reports with the default severity of the rule. An
    // entry that reports nothing, because it is not valid or because its severity is 'none', has no kind and no expression.
    private sealed record BannedSyntaxEntry(TextSpan Span, string Query, string? Message, SyntaxKind? Kind, XPathExpression? Expression, BannedSyntaxTarget Target, DiagnosticSeverity? Severity, string? ErrorMessage)
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
            if (severity is null)
                return CreateEntry(span, query, message, severity: null);

            var (isValid, parsedSeverity) = ParseSeverity(severity);
            if (!isValid)
                return new BannedSyntaxEntry(span, query, message, Kind: null, Expression: null, Target: BannedSyntaxTarget.Syntax, Severity: null, ErrorMessage: $"'{severity}' is not a valid severity");

            var entry = CreateEntry(span, query, message, parsedSeverity);

            // A 'none' severity selects nothing, but the query is still validated
            return parsedSeverity is not null ? entry : entry with { Kind = null, Expression = null };
        }

        private static BannedSyntaxEntry CreateEntry(TextSpan span, string query, string? message, DiagnosticSeverity? severity)
        {
            // A kind name, or "//" followed by a kind name, selects the nodes of this kind, which is faster to find
            // without evaluating a query. A single name is not a useful XPath query, as it selects the root node only
            // when it is of this kind, so it is always interpreted as a kind.
            var kindStart = query.StartsWith("//", StringComparison.Ordinal) ? 2 : 0;
            if (IsKindName(query, kindStart))
            {
                var name = query.Substring(kindStart);
                if (Enum.TryParse<SyntaxKind>(name, ignoreCase: false, out var kind) && kind is not SyntaxKind.None && Enum.IsDefined(typeof(SyntaxKind), kind))
                    return new BannedSyntaxEntry(span, query, message, kind, Expression: null, Target: BannedSyntaxTarget.Syntax, severity, ErrorMessage: null);

                return new BannedSyntaxEntry(span, query, message, Kind: null, Expression: null, Target: BannedSyntaxTarget.Syntax, Severity: null, ErrorMessage: $"'{name}' is not a member of SyntaxKind");
            }

            var (expression, target, errorMessage) = GetQuery(query);
            return new BannedSyntaxEntry(span, query, message, Kind: null, expression, target, severity, errorMessage);
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
        private readonly List<BannedSyntaxEntry> _operationQueries;

        private BannedSyntaxConfiguration(Dictionary<SyntaxKind, List<BannedSyntaxEntry>> kinds, List<BannedSyntaxEntry> queries, List<BannedSyntaxEntry> semanticQueries, List<BannedSyntaxEntry> operationQueries)
        {
            _kinds = kinds;
            _queries = queries;
            _semanticQueries = semanticQueries;
            _operationQueries = operationQueries;
        }

        // The operations are built from the semantic model
        public bool RequiresSemanticModel => _semanticQueries.Count > 0 || _operationQueries.Count > 0;

        public static BannedSyntaxConfiguration? Create(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
        {
            var kinds = new Dictionary<SyntaxKind, List<BannedSyntaxEntry>>();
            var queries = new List<BannedSyntaxEntry>();
            var semanticQueries = new List<BannedSyntaxEntry>();
            var operationQueries = new List<BannedSyntaxEntry>();
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
                        var entries = entry.Target switch
                        {
                            BannedSyntaxTarget.Operations => operationQueries,
                            BannedSyntaxTarget.SemanticSyntax => semanticQueries,
                            _ => queries,
                        };

                        entries.Add(entry);
                    }
                }
            }

            if (kinds.Count is 0 && queries.Count is 0 && semanticQueries.Count is 0 && operationQueries.Count is 0)
                return null;

            return new BannedSyntaxConfiguration(kinds, queries, semanticQueries, operationQueries);
        }

        public void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;
            var reporter = new DiagnosticReporter(context);
            Analyze(tree.GetRoot(context.CancellationToken), semanticModel: null,
                (span, severity, name, message) => Report(reporter, tree, span, severity, name, message), context.CancellationToken);
        }

        public void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var tree = semanticModel.SyntaxTree;
            var reporter = new DiagnosticReporter(context);
            Analyze(tree.GetRoot(context.CancellationToken), semanticModel,
                (span, severity, name, message) => Report(reporter, tree, span, severity, name, message), context.CancellationToken);
        }

        private void Analyze(SyntaxNode root, SemanticModel? semanticModel, Action<TextSpan, DiagnosticSeverity?, string, string> report, CancellationToken cancellationToken)
        {
            // The same syntax can be banned by several entries, possibly from several files
            var reported = new HashSet<(TextSpan Span, DiagnosticSeverity? Severity, string Name, string Message)>();
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

            // The operations of the file are walked once, whatever the number of entries
            if (_operationQueries.Count > 0 && semanticModel is not null)
            {
                var forest = OperationForest.Create(root, semanticModel, cancellationToken);

                // The navigator of the syntax tree is only built when an entry uses the 'syntax' function. It has no
                // semantic model, as the semantic attributes are not available in a query on the operations.
                SyntaxNodeXPathNavigator? syntaxNavigator = null;
                var context = new BannedSyntaxXsltContext(() => syntaxNavigator ??= new SyntaxNodeXPathNavigator(root, cancellationToken));
                Evaluate(_operationQueries, new OperationXPathNavigator(forest, cancellationToken), reported, report, context);
            }
        }

        private static void Evaluate(List<BannedSyntaxEntry> entries, XPathNavigator navigator, HashSet<(TextSpan Span, DiagnosticSeverity? Severity, string Name, string Message)> reported, Action<TextSpan, DiagnosticSeverity?, string, string> report, BannedSyntaxXsltContext? context = null)
        {
            foreach (var entry in entries)
            {
                try
                {
                    // A query using the 'syntax' function selects the operations and the nodes of the syntax tree,
                    // so the matches are not all of the same navigator
                    foreach (XPathNavigator match in navigator.Select(Prepare(entry.Expression!, context)))
                    {
                        // The document that contains the elements is not reportable
                        if (match is not IBannedSyntaxNavigator { ReportName: { } name } reportable)
                            continue;

                        Report(reported, report, reportable.Span, name, entry);
                    }
                }
                catch (XPathException)
                {
                    // The query is validated when the file is parsed, so this only happens for an error that depends on the tree
                }
            }
        }

        private static void Report(HashSet<(TextSpan Span, DiagnosticSeverity? Severity, string Name, string Message)> reported, Action<TextSpan, DiagnosticSeverity?, string, string> report, TextSpan span, string name, BannedSyntaxEntry entry)
        {
            var severity = entry.Severity;
            var message = entry.FormattedMessage;
            if (reported.Add((span, severity, name, message)))
            {
                report(span, severity, name, message);
            }
        }

        // An entry that sets a severity overrides the default severity of the rule
        private static void Report(DiagnosticReporter reporter, SyntaxTree tree, TextSpan span, DiagnosticSeverity? severity, string name, string message)
        {
            var location = Location.Create(tree, span);
            reporter.ReportDiagnostic(severity is { } value
                ? Diagnostic.Create(Rule, location, value, additionalLocations: null, properties: null, name, message)
                : Diagnostic.Create(Rule, location, name, message));
        }
    }
}
