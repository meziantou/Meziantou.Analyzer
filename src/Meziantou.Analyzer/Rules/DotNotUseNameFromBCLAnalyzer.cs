using System.Text.RegularExpressions;
using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DotNotUseNameFromBCLAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.DotNotUseNameFromBCL,
        title: "Do not create a type with a name from the BCL",
        messageFormat: "Type '{0}' exists in namespace '{1}'",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DotNotUseNameFromBCL));

    private static readonly ConfigurationDefinition<bool> OnlyConsiderPublicSymbolsConfiguration = new(RuleIdentifiers.DotNotUseNameFromBCL + ".only_consider_public_symbols", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> UsePreviewTypesConfiguration = new(RuleIdentifiers.DotNotUseNameFromBCL + ".use_preview_types", defaultValue: false);
    internal static readonly ConfigurationDefinition<string> NamespacesRegexConfiguration = new(RuleIdentifiers.DotNotUseNameFromBCL + ".namespaces_regex", defaultValue: "^System($|\\.)") { RegexOptions = RegexOptions.None };
    internal static readonly ConfigurationDefinition<string> LegacyNamepacesRegexConfiguration = new(RuleIdentifiers.DotNotUseNameFromBCL + ".namepaces_regex", defaultValue: "^System($|\\.)") { IsHidden = true, RegexOptions = RegexOptions.None };

    // The tables are big, so they are only loaded when the rule runs, and only the configured one is loaded
    private static readonly Lazy<Dictionary<string, string[]>> Types = new(() => LoadTypes(preview: false));
    private static readonly Lazy<Dictionary<string, string[]>> PreviewTypes = new(() => LoadTypes(preview: true));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.ContainingType is not null)
            return; // Do not consider nested types

        if (!symbol.IsVisibleOutsideOfAssembly())
        {
            if (context.Options.GetConfigurationValue(symbol, OnlyConsiderPublicSymbolsConfiguration))
                return;
        }

        var usePreviewTypes = context.Options.GetConfigurationValue(symbol, UsePreviewTypesConfiguration);
        var types = usePreviewTypes ? PreviewTypes.Value : Types.Value;
        if (types.TryGetValue(symbol.MetadataName, out var namespaces))
        {
            var namespaceRegex = GetNamespacesRegex(context, symbol);
            if (namespaceRegex is null)
                return;

            foreach (var ns in namespaces)
            {
                if (RegexCache.IsMatch(namespaceRegex, ns, defaultValue: false))
                {
                    context.ReportDiagnostic(Rule, symbol, symbol.MetadataName, ns);
                    return;
                }
            }
        }
    }

    private static Regex? GetNamespacesRegex(SymbolAnalysisContext context, ISymbol symbol)
    {
        // The legacy option is only used when the current one is not configured. An invalid pattern falls back to the
        // default pattern instead of failing the analysis.
        var configuration = context.Options.TryGetConfigurationValue(symbol, NamespacesRegexConfiguration, out _)
            ? NamespacesRegexConfiguration
            : LegacyNamepacesRegexConfiguration;

        return context.Options.GetConfigurationRegex(symbol, configuration);
    }

    private static Dictionary<string, string[]> LoadTypes(bool preview)
    {
        var types = new Dictionary<string, string[]>(StringComparer.Ordinal);

        var resourceName = preview ? "Meziantou.Analyzer.Resources.bcl-preview.txt" : "Meziantou.Analyzer.Resources.bcl.txt";
        using var stream = typeof(DotNotUseNameFromBCLAnalyzer).Assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return types;

        // The same few hundred namespaces are repeated on thousands of lines, so they are deduplicated to keep a single instance of each
        var knownNamespaces = new Dictionary<string, string>(StringComparer.Ordinal);

        using var sr = new StreamReader(stream);
        while (sr.ReadLine() is { } line)
        {
            var index = line.LastIndexOf('.', StringComparison.Ordinal);
            var ns = line[..index];
            var name = line[(index + 1)..];

            if (knownNamespaces.TryGetValue(ns, out var knownNamespace))
            {
                ns = knownNamespace;
            }
            else
            {
                knownNamespaces.Add(ns, ns);
            }

            // Very few names are declared in multiple namespaces, so the arrays are sized exactly instead of using a list
            types[name] = types.TryGetValue(name, out var existingNamespaces) ? [.. existingNamespaces, ns] : [ns];
        }

        return types;
    }
}
