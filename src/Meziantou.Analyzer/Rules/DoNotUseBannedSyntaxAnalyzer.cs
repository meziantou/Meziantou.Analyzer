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

    // The name of the axis that selects the attributes, which is how a query selects them without naming them
    private const string AttributeAxisName = "attribute";

    private static readonly char[] DirectorySeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    // The files are parsed by the compilation start action and by the additional file action, so the result is shared
    private static readonly ConditionalWeakTable<SourceText, BannedSyntaxFile> ParsedFiles = new();

    // The queries come from the files of the analyzed projects, and an editor provides a new content at every keystroke
    private static readonly BoundedCache<string, ParsedQuery> QueryCache = new(capacity: 128);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule, InvalidEntryRule);

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

    // The name of the file is compared in place, as the rule is enabled by default and every project pays for the
    // test, whereas only the projects that have a banned syntax file pay for anything else
    private static bool IsBannedSyntaxFile(string path)
    {
        var start = path.LastIndexOfAny(DirectorySeparators) + 1;
        var length = path.Length - start;
        if (length == FileName.Length && string.Compare(path, start, FileName, 0, FileName.Length, StringComparison.OrdinalIgnoreCase) is 0)
            return true;

        return length >= FileNamePrefix.Length
            && length >= FileNameExtension.Length
            && string.Compare(path, start, FileNamePrefix, 0, FileNamePrefix.Length, StringComparison.OrdinalIgnoreCase) is 0
            && string.Compare(path, path.Length - FileNameExtension.Length, FileNameExtension, 0, FileNameExtension.Length, StringComparison.OrdinalIgnoreCase) is 0;
    }

    private static BannedSyntaxFile? GetParsedFile(AdditionalText additionalText, CancellationToken cancellationToken)
    {
        var text = additionalText.GetText(cancellationToken);
        if (text is null)
            return null;

        return ParsedFiles.GetValue(text, BannedSyntaxFile.Parse);
    }

    private static ParsedQuery GetQuery(string query)
    {
        return QueryCache.GetOrAdd(query, static query =>
        {
            try
            {
                // The context defines the 'semantic', 'operation' and 'symbol' prefixes, the 'syntax' and 'symbol'
                // functions, and the semantic functions, such as 'implements'. An undefined prefix throws when the query is compiled, and an expression that is not
                // compiled with an XsltContext cannot use a function of its own, whatever the context it is evaluated with.
                var expression = XPathExpression.Compile(query, BannedSyntaxXsltContext.Empty);
                if (expression.ReturnType is not XPathResultType.NodeSet)
                    return new ParsedQuery(Expression: null, BannedSyntaxTarget.Syntax, "The query must return a node-set", AttributeNames: null);

                var (target, errorMessage, attributeNames) = ScanNames(query);
                if (errorMessage is not null)
                    return new ParsedQuery(Expression: null, BannedSyntaxTarget.Syntax, errorMessage, AttributeNames: null);

                // Some errors, such as an unknown function, are only detected when the query is evaluated. The empty
                // document has no semantic model, no operation and no symbol, so those attributes are simply not exposed.
                XPathNavigator navigator = target switch
                {
                    BannedSyntaxTarget.Operations => new OperationXPathNavigator(OperationForest.Empty, CancellationToken.None),
                    BannedSyntaxTarget.Symbols => new SymbolXPathNavigator(SymbolForest.Empty),
                    _ => new SyntaxNodeXPathNavigator(new SyntaxForest(SyntaxFactory.CompilationUnit(), semanticModel: null, XPathAttributeFilter.All, CancellationToken.None)),
                };

                foreach (var _ in navigator.Select(Prepare(expression, BannedSyntaxXsltContext.Empty)))
                {
                }

                return new ParsedQuery(expression, target, ErrorMessage: null, attributeNames);
            }
            catch (XPathException ex)
            {
                return new ParsedQuery(Expression: null, BannedSyntaxTarget.Syntax, ex.Message, AttributeNames: null);
            }
        });
    }

    // A query using an attribute of the 'semantic' namespace needs the semantic model, a query using an element of
    // the 'operation' namespace is evaluated on the operations, and a query using an element of the 'symbol' namespace
    // is evaluated on the symbols. The functions return the nodes of another document, but the names of a query that
    // are not prefixed can be the ones of several documents, so the document the query starts on is decided by the
    // names it uses. An unknown name would silently match nothing, so it is reported instead. The names of the
    // attributes the query selects are collected too, as computing an attribute costs a call to the semantic model or
    // a reflection call.
    private static (BannedSyntaxTarget Target, string? ErrorMessage, HashSet<string>? AttributeNames) ScanNames(string query)
    {
        var usesSemanticModel = false;
        var usesOperations = false;
        var usesSymbols = false;
        var usesSyntaxFunction = false;
        var usesSymbolFunction = false;
        var usesSemanticFunction = false;

        // null once the query selects attributes it does not name, so all of them must be computed
        var attributeNames = new HashSet<string>(StringComparer.Ordinal);
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

            // '@*' and '@prefix:*' select the attributes the query does not name
            if (c is '@' && IsWildcard(query, i + 1))
            {
                attributeNames = null;
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
            {
                // The 'attribute' axis selects the attributes without naming them, as in 'attribute::*'
                if (IsAxis(query, end))
                {
                    if (IsName(query, start, end, AttributeAxisName))
                    {
                        attributeNames = null;
                    }

                    continue;
                }

                // A name followed by '(' is a function or a node type test, such as 'node()'
                if (IsFunctionCall(query, end))
                {
                    if (IsName(query, start, end, BannedSyntaxXsltContext.SyntaxFunctionName))
                    {
                        usesSyntaxFunction = true;
                    }
                    else if (IsName(query, start, end, BannedSyntaxXsltContext.SymbolFunctionName))
                    {
                        usesSymbolFunction = true;
                    }
                    else if (BannedSyntaxXsltContext.IsSemanticFunctionName(query.Substring(start, end - start)))
                    {
                        usesSemanticFunction = true;

                        // An unknown format would silently match nothing, so it is reported when it is a literal
                        if (BannedSyntaxXsltContext.IsTypeNameFunctionName(query.Substring(start, end - start)) && GetSecondStringArgument(query, end) is { } format && !XPathTypeNameMatcher.IsFormatName(format))
                            return (BannedSyntaxTarget.SemanticSyntax, $"'{format}' is not a valid type name format. The valid formats are '{XPathTypeNameMatcher.MetadataNameFormat}', '{XPathTypeNameMatcher.DocumentationDeclarationIdFormat}' and '{XPathTypeNameMatcher.DocumentationReferenceIdFormat}'", AttributeNames: null);
                    }
                    else if (IsAttributeName(query, start))
                    {
                        // '@node()' selects the attributes the query does not name
                        attributeNames = null;
                    }

                    continue;
                }

                if (attributeNames is not null && IsAttributeName(query, start))
                {
                    attributeNames.Add(query.Substring(start, end - start));
                }

                continue;
            }

            var prefix = query.Substring(start, end - start);
            var isSemantic = string.Equals(prefix, XPathNamespaces.SemanticPrefix, StringComparison.Ordinal);
            var isOperation = string.Equals(prefix, XPathNamespaces.OperationPrefix, StringComparison.Ordinal);
            var isSymbol = string.Equals(prefix, XPathNamespaces.SymbolPrefix, StringComparison.Ordinal);

            // The context resolves the prefixes when the query is evaluated, so an undefined prefix selects nothing
            // instead of throwing when the query is compiled
            if (!isSemantic && !isOperation && !isSymbol)
                return (BannedSyntaxTarget.Syntax, $"'{prefix}' is not a defined namespace prefix", AttributeNames: null);

            usesSemanticModel |= isSemantic;
            usesOperations |= isOperation;
            usesSymbols |= isSymbol;

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
                    return (BannedSyntaxTarget.SemanticSyntax, $"'{name}' is not a valid semantic attribute", AttributeNames: null);

                // The semantic attributes are always prefixed, so they are the only prefixed attributes
                if (attributeNames is not null && IsAttributeName(query, start))
                {
                    attributeNames.Add(name);
                }
            }
            else if (isOperation && name.Length > 0 && !IsOperationKindName(name))
            {
                // 'operation:*' selects every operation, so it has no name to validate
                return (BannedSyntaxTarget.Operations, $"'{name}' is not a kind of operation", AttributeNames: null);
            }
            else if (isSymbol && name.Length > 0 && !SymbolXPathNavigator.IsKindName(name))
            {
                // The name must be one of the kinds of the symbols the tree contains, so a kind such as 'Label' is
                // reported instead of silently selecting nothing
                return (BannedSyntaxTarget.Symbols, $"'{name}' is not a kind of symbol", AttributeNames: null);
            }

            i = nameEnd - 1;
        }

        // The 'symbol' function takes syntax nodes, so a query using it starts on the syntax tree, with the semantic
        // model the symbols come from. The operations are another document the query cannot reach.
        if (usesSymbolFunction)
        {
            if (usesOperations)
                return (BannedSyntaxTarget.SemanticSyntax, $"A query using the '{BannedSyntaxXsltContext.SymbolFunctionName}' function cannot use the '{XPathNamespaces.OperationPrefix}' prefix", AttributeNames: null);

            return (BannedSyntaxTarget.SemanticSyntax, null, attributeNames);
        }

        // A query is evaluated on a single document, and the operations and the symbols expose no semantic attribute
        if (usesSymbols)
        {
            if (usesSemanticModel || usesOperations)
                return (BannedSyntaxTarget.Symbols, $"A query cannot use both the '{XPathNamespaces.SymbolPrefix}' and the '{(usesSemanticModel ? XPathNamespaces.SemanticPrefix : XPathNamespaces.OperationPrefix)}' prefixes", AttributeNames: null);

            return (BannedSyntaxTarget.Symbols, null, attributeNames);
        }

        // The 'syntax' function returns the nodes of the operations when the query does not use the symbols
        if (usesOperations || usesSyntaxFunction)
        {
            if (usesSemanticModel)
            {
                return (BannedSyntaxTarget.Operations, usesOperations
                    ? $"A query cannot use both the '{XPathNamespaces.SemanticPrefix}' and the '{XPathNamespaces.OperationPrefix}' prefixes"
                    : $"A query using the '{BannedSyntaxXsltContext.SyntaxFunctionName}' function without the '{XPathNamespaces.SymbolPrefix}' prefix is evaluated on the operations, so it cannot use the '{XPathNamespaces.SemanticPrefix}' prefix", AttributeNames: null);
            }

            return (BannedSyntaxTarget.Operations, null, attributeNames);
        }

        // The semantic functions, such as 'implements', need the semantic model, which the operations and the symbols
        // already have
        return (usesSemanticModel || usesSemanticFunction ? BannedSyntaxTarget.SemanticSyntax : BannedSyntaxTarget.Syntax, null, attributeNames);
    }

    // The second argument of the function call whose name ends at this index, when it is a string literal, such as
    // the format of 'implements('System.IDisposable', 'MetadataName')'. The arguments can contain function calls,
    // predicates and literals, so only the ',' that are not nested separate the arguments.
    private static string? GetSecondStringArgument(string query, int index)
    {
        while (index < query.Length && query[index] is not '(')
        {
            index++;
        }

        var depth = 0;
        var quote = '\0';
        for (var i = index + 1; i < query.Length; i++)
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

            switch (c)
            {
                case '\'' or '"':
                    quote = c;
                    break;

                case '(' or '[':
                    depth++;
                    break;

                case ')' or ']':
                    if (depth is 0)
                        return null;

                    depth--;
                    break;

                case ',' when depth is 0:
                    var start = i + 1;
                    while (start < query.Length && char.IsWhiteSpace(query[start]))
                    {
                        start++;
                    }

                    if (start >= query.Length || query[start] is not ('\'' or '"'))
                        return null;

                    var end = query.IndexOf(query[start], start + 1, StringComparison.Ordinal);
                    return end < 0 ? null : query.Substring(start + 1, end - start - 1);
            }
        }

        return null;
    }

    // The name is preceded by '@', so it is the name of an attribute and not the name of an element
    private static bool IsAttributeName(string query, int index)
    {
        while (index > 0 && char.IsWhiteSpace(query[index - 1]))
        {
            index--;
        }

        return index > 0 && query[index - 1] is '@';
    }

    // The name is followed by '::', so it is the name of an axis
    private static bool IsAxis(string query, int index) => index + 1 < query.Length && query[index] is ':' && query[index + 1] is ':';

    // The value at this index is '*', possibly preceded by whitespace
    private static bool IsWildcard(string query, int index)
    {
        while (index < query.Length && char.IsWhiteSpace(query[index]))
        {
            index++;
        }

        return index < query.Length && query[index] is '*';
    }

    // The range of the query is this name
    private static bool IsName(string query, int start, int end, string name)
        => end - start == name.Length && string.CompareOrdinal(query, start, name, 0, name.Length) is 0;

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
    // the 'syntax' and 'symbol' functions of the file that is analyzed.
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

    // The result of the parsing of a query. The names of the attributes it selects are null when it selects the
    // attributes it does not name, such as with '@*', so all of them must be computed.
    private sealed record ParsedQuery(XPathExpression? Expression, BannedSyntaxTarget Target, string? ErrorMessage, HashSet<string>? AttributeNames);

    // The document a query starts on. The 'syntax' and 'symbol' functions return the nodes of another document.
    private enum BannedSyntaxTarget
    {
        Syntax,
        SemanticSyntax,
        Operations,
        Symbols,
    }

    // The severity is null when the entry does not set one, so it reports with the default severity of the rule. An
    // entry that reports nothing, because it is not valid or because its severity is 'none', has no kind and no expression.
    private sealed record BannedSyntaxEntry(TextSpan Span, string Query, string? Message, SyntaxKind? Kind, XPathExpression? Expression, BannedSyntaxTarget Target, DiagnosticSeverity? Severity, string? ErrorMessage, HashSet<string>? AttributeNames = null)
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

            var parsed = GetQuery(query);
            return new BannedSyntaxEntry(span, query, message, Kind: null, parsed.Expression, parsed.Target, severity, parsed.ErrorMessage, parsed.AttributeNames);
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
        private readonly List<BannedSyntaxEntry> _symbolQueries;

        // The attributes the entries of each document can select, so the other ones are never computed
        private readonly XPathAttributeFilter _queryFilter;
        private readonly XPathAttributeFilter _semanticQueryFilter;
        private readonly XPathAttributeFilter _operationQueryFilter;
        private readonly XPathAttributeFilter _symbolQueryFilter;

        private BannedSyntaxConfiguration(Dictionary<SyntaxKind, List<BannedSyntaxEntry>> kinds, List<BannedSyntaxEntry> queries, List<BannedSyntaxEntry> semanticQueries, List<BannedSyntaxEntry> operationQueries, List<BannedSyntaxEntry> symbolQueries)
        {
            _kinds = kinds;
            _queries = queries;
            _semanticQueries = semanticQueries;
            _operationQueries = operationQueries;
            _symbolQueries = symbolQueries;
            _queryFilter = CreateFilter(queries);
            _semanticQueryFilter = CreateFilter(semanticQueries);
            _operationQueryFilter = CreateFilter(operationQueries);
            _symbolQueryFilter = CreateFilter(symbolQueries);
        }

        // An entry that selects the attributes it does not name needs all of them, and the other ones only need the
        // attributes they name
        private static XPathAttributeFilter CreateFilter(List<BannedSyntaxEntry> entries)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry.AttributeNames is null)
                    return XPathAttributeFilter.All;

                names.UnionWith(entry.AttributeNames);
            }

            return XPathAttributeFilter.Create(names);
        }

        // The operations and the symbols are built from the semantic model
        public bool RequiresSemanticModel => _semanticQueries.Count > 0 || _operationQueries.Count > 0 || _symbolQueries.Count > 0;

        public static BannedSyntaxConfiguration? Create(ImmutableArray<AdditionalText> additionalFiles, CancellationToken cancellationToken)
        {
            var kinds = new Dictionary<SyntaxKind, List<BannedSyntaxEntry>>();
            var queries = new List<BannedSyntaxEntry>();
            var semanticQueries = new List<BannedSyntaxEntry>();
            var operationQueries = new List<BannedSyntaxEntry>();
            var symbolQueries = new List<BannedSyntaxEntry>();
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
                            BannedSyntaxTarget.Symbols => symbolQueries,
                            BannedSyntaxTarget.SemanticSyntax => semanticQueries,
                            _ => queries,
                        };

                        entries.Add(entry);
                    }
                }
            }

            if (kinds.Count is 0 && queries.Count is 0 && semanticQueries.Count is 0 && operationQueries.Count is 0 && symbolQueries.Count is 0)
                return null;

            return new BannedSyntaxConfiguration(kinds, queries, semanticQueries, operationQueries, symbolQueries);
        }

        public void AnalyzeTree(SyntaxTreeAnalysisContext context)
        {
            var tree = context.Tree;
            Analyze(tree.GetRoot(context.CancellationToken), semanticModel: null, new ReportSink(context, tree), context.CancellationToken);
        }

        public void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var tree = semanticModel.SyntaxTree;
            Analyze(tree.GetRoot(context.CancellationToken), semanticModel, new ReportSink(context, tree), context.CancellationToken);
        }

        private void Analyze(SyntaxNode root, SemanticModel? semanticModel, ReportSink sink, CancellationToken cancellationToken)
        {
            if (_kinds.Count > 0)
            {
                foreach (var node in root.DescendantNodesAndSelf())
                {
                    var kind = node.Kind();
                    if (_kinds.TryGetValue(kind, out var entries))
                    {
                        foreach (var entry in entries)
                        {
                            sink.Report(node.Span, kind.ToString(), entry);
                        }
                    }
                }
            }

            // The entries that do not use the semantic model are evaluated on a navigator that does not expose the
            // semantic attributes, so they do not pay for them
            if (_queries.Count > 0)
            {
                var forest = new SyntaxForest(root, semanticModel: null, _queryFilter, cancellationToken);
                Evaluate(_queries, new SyntaxNodeXPathNavigator(forest), sink);
            }

            if (_semanticQueries.Count > 0 && semanticModel is not null)
            {
                var forest = new SyntaxForest(root, semanticModel, _semanticQueryFilter, cancellationToken);
                var navigator = new SyntaxNodeXPathNavigator(forest);

                // The symbols are only built when an entry uses the 'symbol' function. The 'syntax' function goes back
                // to the same tree, so the semantic attributes are still available after a round trip.
                SymbolXPathNavigator? symbolNavigator = null;
                var context = new BannedSyntaxXsltContext(() => navigator, () => symbolNavigator ??= new SymbolXPathNavigator(SymbolForest.Create(root, semanticModel, _semanticQueryFilter, cancellationToken)), semanticModel, cancellationToken);
                Evaluate(_semanticQueries, navigator, sink, context);
            }

            // The operations of the file are walked once, whatever the number of entries
            if (_operationQueries.Count > 0 && semanticModel is not null)
            {
                var forest = OperationForest.Create(root, semanticModel, _operationQueryFilter, cancellationToken);

                // The navigator of the syntax tree is only built when an entry uses the 'syntax' function. It has no
                // semantic model, as the semantic attributes are not available in a query on the operations.
                SyntaxNodeXPathNavigator? syntaxNavigator = null;
                var context = new BannedSyntaxXsltContext(() => syntaxNavigator ??= new SyntaxNodeXPathNavigator(new SyntaxForest(root, semanticModel: null, _operationQueryFilter, cancellationToken)), symbolNavigatorFactory: null, semanticModel, cancellationToken);
                Evaluate(_operationQueries, new OperationXPathNavigator(forest, cancellationToken), sink, context);
            }

            // The symbols declared in the file are collected once, whatever the number of entries
            if (_symbolQueries.Count > 0 && semanticModel is not null)
            {
                var forest = SymbolForest.Create(root, semanticModel, _symbolQueryFilter, cancellationToken);

                // Like for the operations, the navigator of the syntax tree is only built when an entry uses the
                // 'syntax' function, and it has no semantic model
                SyntaxNodeXPathNavigator? syntaxNavigator = null;
                var context = new BannedSyntaxXsltContext(() => syntaxNavigator ??= new SyntaxNodeXPathNavigator(new SyntaxForest(root, semanticModel: null, _symbolQueryFilter, cancellationToken)), symbolNavigatorFactory: null, semanticModel, cancellationToken);
                Evaluate(_symbolQueries, new SymbolXPathNavigator(forest), sink, context);
            }
        }

        private static void Evaluate(List<BannedSyntaxEntry> entries, XPathNavigator navigator, ReportSink sink, BannedSyntaxXsltContext? context = null)
        {
            foreach (var entry in entries)
            {
                try
                {
                    // A query using the 'syntax' or the 'symbol' function selects the nodes of several documents, so the
                    // matches are not all of the same navigator
                    foreach (XPathNavigator match in navigator.Select(Prepare(entry.Expression!, context)))
                    {
                        // The document that contains the elements is not reportable
                        if (match is not IBannedSyntaxNavigator { ReportName: { } name } reportable)
                            continue;

                        // A symbol is reported on each of its declarations in the file
                        foreach (var span in reportable.ReportSpans)
                        {
                            sink.Report(span, name, entry);
                        }
                    }
                }
                catch (XPathException)
                {
                    // The query is validated when the file is parsed, so this only happens for an error that depends on the tree
                }
            }
        }

        // The diagnostics of a file. Most of the files have no match at all, so the set of the diagnostics that were
        // already reported is only allocated when there is one.
        private sealed class ReportSink(DiagnosticReporter reporter, SyntaxTree tree)
        {
            // The same syntax can be banned by several entries, possibly from several files
            private HashSet<(TextSpan Span, DiagnosticSeverity? Severity, string Name, string Message)>? _reported;

            // An entry that sets a severity overrides the default severity of the rule
            public void Report(TextSpan span, string name, BannedSyntaxEntry entry)
            {
                var severity = entry.Severity;
                var message = entry.FormattedMessage;
                _reported ??= [];
                if (!_reported.Add((span, severity, name, message)))
                    return;

                var location = Location.Create(tree, span);
                reporter.ReportDiagnostic(severity is { } value
                    ? Diagnostic.Create(Rule, location, value, additionalLocations: null, properties: null, name, message)
                    : Diagnostic.Create(Rule, location, name, message));
            }
        }
    }
}
