using System.Xml.XPath;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseBannedSyntaxKindAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DoNotUseBannedSyntaxKind,
        title: "Do not use banned syntax",
        messageFormat: "The syntax '{0}' is banned",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBannedSyntaxKind));

    // Uses the same id as the rule, so the invalid queries are reported only to the projects that enable the rule
    private static readonly DiagnosticDescriptor InvalidQueryRule = new(
        RuleIdentifiers.DoNotUseBannedSyntaxKind,
        title: "Do not use banned syntax",
        messageFormat: "The value of '{0}' is not a valid XPath query: {1}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DoNotUseBannedSyntaxKind));

    private static readonly ConfigurationDefinition<string> SyntaxKindsConfiguration = new(RuleIdentifiers.DoNotUseBannedSyntaxKind + ".syntax_kinds", defaultValue: "");
    private static readonly ConfigurationDefinition<string> QueryConfiguration = new(RuleIdentifiers.DoNotUseBannedSyntaxKind + ".query", defaultValue: "");

    private static readonly char[] Separators = [',', ' ', '\t'];

    // The queries come from the configuration, and an editor provides a new value at every keystroke
    private static readonly BoundedCache<string, (XPathExpression? Expression, string? ErrorMessage)> QueryCache = new(capacity: 32);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, InvalidQueryRule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxTreeAction(AnalyzeTree);
        context.RegisterCompilationAction(ValidateQueries);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var bannedKinds = ParseSyntaxKinds(context.Options.GetConfigurationValue(context.Tree, SyntaxKindsConfiguration));
        var query = context.Options.GetConfigurationValue(context.Tree, QueryConfiguration);
        var expression = GetQuery(query).Expression;
        if (bannedKinds.Count is 0 && expression is null)
            return;

        var root = context.Tree.GetRoot(context.CancellationToken);

        // A node can be matched by both options
        var reported = new HashSet<(TextSpan Span, string Name)>();
        if (bannedKinds.Count > 0)
        {
            foreach (var node in root.DescendantNodesAndSelf())
            {
                var kind = node.Kind();
                if (bannedKinds.Contains(kind))
                {
                    Report(context, reported, node.Span, kind.ToString());
                }
            }
        }

        if (expression is not null)
        {
            try
            {
                // A compiled expression is not thread-safe, so each evaluation uses its own copy
                var navigator = new SyntaxNodeXPathNavigator(root, context.CancellationToken);
                foreach (SyntaxNodeXPathNavigator match in navigator.Select(expression.Clone()))
                {
                    if (match.Node is null)
                        continue;

                    var name = match.Node.Kind().ToString();
                    if (match.AttributeName is { } attributeName)
                    {
                        name += "/@" + attributeName;
                    }

                    Report(context, reported, match.Span, name);
                }
            }
            catch (XPathException)
            {
                // Reported by ValidateQueries
            }
        }
    }

    private static void Report(SyntaxTreeAnalysisContext context, HashSet<(TextSpan Span, string Name)> reported, TextSpan span, string name)
    {
        if (reported.Add((span, name)))
        {
            context.ReportDiagnostic(Rule, Location.Create(context.Tree, span), name);
        }
    }

    private static void ValidateQueries(CompilationAnalysisContext context)
    {
        // The options can be different for each syntax tree, but a value must be reported only once
        HashSet<string>? reportedValues = null;
        foreach (var syntaxTree in context.Compilation.SyntaxTrees)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var query = context.Options.GetConfigurationValue(syntaxTree, QueryConfiguration);
            if (string.IsNullOrWhiteSpace(query))
                continue;

            reportedValues ??= new(StringComparer.Ordinal);
            if (!reportedValues.Add(query))
                continue;

            if (GetQuery(query).ErrorMessage is { } errorMessage)
            {
                context.ReportDiagnostic(InvalidQueryRule, Location.None, QueryConfiguration.Key, errorMessage);
            }
        }
    }

    private static (XPathExpression? Expression, string? ErrorMessage) GetQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (null, null);

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

    internal static HashSet<SyntaxKind> ParseSyntaxKinds(string value)
    {
        var result = new HashSet<SyntaxKind>();
        foreach (var part in value.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            // Enum.TryParse accepts numeric values, which do not identify a kind across Roslyn versions
            if (part[0] is >= '0' and <= '9' or '-' or '+')
                continue;

            if (Enum.TryParse<SyntaxKind>(part, ignoreCase: true, out var kind) && kind is not SyntaxKind.None && Enum.IsDefined(typeof(SyntaxKind), kind))
            {
                result.Add(kind);
            }
        }

        return result;
    }
}
