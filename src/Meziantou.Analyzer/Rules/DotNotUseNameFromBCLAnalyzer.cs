using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DotNotUseNameFromBCLAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
        RuleIdentifiers.DotNotUseNameFromBCL,
        title: "Do not create a type with a name from the BCL",
        messageFormat: "Type '{0}' exists in namespace '{1}'",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.DotNotUseNameFromBCL));

    private static Dictionary<string, List<string>>? s_types;
    private static Dictionary<string, List<string>>? s_typesPreview;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

    public override void Initialize(AnalysisContext context)
    {
        s_types ??= LoadTypes(preview: false);
        s_typesPreview ??= LoadTypes(preview: true);

        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = (INamedTypeSymbol)context.Symbol;
        if (symbol.ContainingType != null)
            return; // Do not consider nested types

        var usePreviewTypes = context.Options.GetConfigurationValue(symbol, RuleIdentifiers.DotNotUseNameFromBCL + ".use_preview_types", defaultValue: false);
        var types = usePreviewTypes ? s_typesPreview : s_types;
        if (types!.TryGetValue(symbol.MetadataName, out var namespaces))
        {
            var regex = context.Options.GetConfigurationValue(symbol, RuleIdentifiers.DotNotUseNameFromBCL + ".namepaces_regex", "^System($|\\.)");
            foreach (var ns in namespaces)
            {
                if (Regex.IsMatch(ns, regex, RegexOptions.None, Timeout.InfiniteTimeSpan))
                {
                    context.ReportDiagnostic(s_rule, symbol, symbol.MetadataName, ns);
                    return;
                }
            }
        }
    }

    private static Dictionary<string, List<string>> LoadTypes(bool preview)
    {
        var types = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        var resourceName = preview ? "Meziantou.Analyzer.Resources.bcl-preview.txt" : "Meziantou.Analyzer.Resources.bcl.txt";
        using var stream = typeof(DotNotUseNameFromBCLAnalyzer).Assembly.GetManifestResourceStream(resourceName)!;
        using var sr = new StreamReader(stream);
        while (sr.ReadLine() is { } line)
        {
            var index = line.LastIndexOf('.');
            var ns = line.Substring(0, index);
            var name = line.Substring(index + 1);

            if (!types.TryGetValue(name, out var list))
            {
                list = new List<string>();
                types.Add(name, list);
            }

            list.Add(ns);
        }

        return types;
    }
}
