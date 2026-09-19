using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis.CSharp;

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

    private static readonly ConfigurationDefinition<string> SyntaxKindsConfiguration = new(RuleIdentifiers.DoNotUseBannedSyntaxKind + ".syntax_kinds", defaultValue: "");

    private static readonly char[] Separators = [',', ' ', '\t'];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureAnalysisOfGeneratedCode(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxTreeAction(AnalyzeTree);
    }

    private static void AnalyzeTree(SyntaxTreeAnalysisContext context)
    {
        var configuration = context.Options.GetConfigurationValue(context.Tree, SyntaxKindsConfiguration);
        var bannedKinds = ParseSyntaxKinds(configuration);
        if (bannedKinds.Count is 0)
            return;

        var root = context.Tree.GetRoot(context.CancellationToken);
        foreach (var node in root.DescendantNodesAndSelf())
        {
            var kind = node.Kind();
            if (bannedKinds.Contains(kind))
            {
                context.ReportDiagnostic(Rule, node.GetLocation(), kind.ToString());
            }
        }
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
