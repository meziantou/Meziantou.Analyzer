using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DocumentationGenerator;

internal static class Program
{
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("You must specify the output folder");
            return;
        }

        var outputFolder = Path.GetFullPath(args[0]);

        var assemblies = new[] { typeof(Meziantou.Analyzer.Rules.CommaAnalyzer).Assembly, typeof(Meziantou.Analyzer.Rules.CommaFixer).Assembly };
        var diagnosticAnalyzers = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
            .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
            .ToList();

        var codeFixProviders = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => !type.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(type))
            .Select(type => (CodeFixProvider)Activator.CreateInstance(type)!)
            .ToList();

        var diagnosticSuppressors = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
          .Where(type => !type.IsAbstract && typeof(DiagnosticSuppressor).IsAssignableFrom(type))
          .Select(type => (DiagnosticSuppressor)Activator.CreateInstance(type)!)
          .ToList();

        var sb = new StringBuilder();
        sb.Append("# ").Append(assemblies.First().GetName().Name).Append("'s rules\n");
        var rulesTable = GenerateRulesTable(diagnosticAnalyzers, codeFixProviders);
        sb.Append(rulesTable);

        var suppressorsTable = GenerateSuppressorsTable(diagnosticSuppressors);
        sb.Append('\n');
        sb.Append(suppressorsTable);

        sb.Append("\n\n# .editorconfig - default values\n\n");
        GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: null);

        sb.Append("\n# .editorconfig - all rules disabled\n\n");
        GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: "none");

        Console.WriteLine(sb.ToString());

        // Update home readme
        {
            // The main readme is embedded into the NuGet package and rendered by nuget.org.
            // nuget.org's markdown support is limited. Raw html in table is not supported.
            var readmePath = Path.GetFullPath(Path.Combine(outputFolder, "README.md"));
            var readmeContent = File.ReadAllText(readmePath);
            var newContent = Regex.Replace(readmeContent, "(?<=<!-- rules -->\\r?\\n).*(?=<!-- rules -->)", "\n" + GenerateRulesTable(diagnosticAnalyzers, codeFixProviders, false) + "\n", RegexOptions.Singleline);
            newContent = Regex.Replace(newContent, "(?<=<!-- suppressions -->\\r?\\n).*(?=<!-- suppressions -->)", "\n" + GenerateSuppressorsTable(diagnosticSuppressors) + "\n", RegexOptions.Singleline);
            File.WriteAllText(readmePath, newContent);
        }

        // Update doc readme
        {
            var path = Path.GetFullPath(Path.Combine(outputFolder, "docs", "README.md"));
            Console.WriteLine(path);
            File.WriteAllText(path, sb.ToString());
        }

        // Update title in rule pages
        {
            foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id))
            {
                var title = $"# {diagnostic.Id} - {EscapeMarkdown(diagnostic.Title.ToString(CultureInfo.InvariantCulture))}";
                var detailPath = Path.GetFullPath(Path.Combine(outputFolder, "docs", "Rules", diagnostic.Id + ".md"));
                if (File.Exists(detailPath))
                {
                    var lines = File.ReadAllLines(detailPath);
                    lines[0] = title;
                    File.WriteAllLines(detailPath, lines);
                }
                else
                {
                    File.WriteAllText(detailPath, title);
                }
            }
        }
    }

    private static string GenerateRulesTable(List<DiagnosticAnalyzer> diagnosticAnalyzers, List<CodeFixProvider> codeFixProviders, bool addTitle = true)
    {
        var sb = new StringBuilder();
        sb.Append("|Id|Category|Description|Severity|Is enabled|Code fix|\n");
        sb.Append("|--|--------|-----------|:------:|:--------:|:------:|\n");

        foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id, StringComparer.Ordinal))
        {
            if (!diagnostic.HelpLinkUri.Contains(diagnostic.Id, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid help link for " + diagnostic.Id);
            }

            var hasCodeFix = codeFixProviders.Any(codeFixProvider => codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id));
            sb.Append("|[")
              .Append(diagnostic.Id)
              .Append("](")
              .Append(diagnostic.HelpLinkUri)
              .Append(")|")
              .Append(diagnostic.Category)
              .Append('|')
              .Append(EscapeMarkdown(diagnostic.Title.ToString(CultureInfo.InvariantCulture)))
              .Append('|');
            if (addTitle)
            {
                sb.Append("<span title='")
                  .Append(HtmlEncoder.Default.Encode(diagnostic.DefaultSeverity.ToString()))
                  .Append("'>")
                  .Append(GetSeverity(diagnostic.DefaultSeverity))
                  .Append("</span>");
            }
            else
            {
                sb.Append(GetSeverity(diagnostic.DefaultSeverity));
            }

            sb.Append('|')
              .Append(GetBoolean(diagnostic.IsEnabledByDefault))
              .Append('|')
              .Append(GetBoolean(hasCodeFix))
              .Append('|')
              .Append('\n');
        }

        return sb.ToString();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The url must be lowercase")]
    private static string GenerateSuppressorsTable(List<DiagnosticSuppressor> diagnosticSuppressors)
    {
        var sb = new StringBuilder();
        sb.Append("|Id|Suppressed rule|Justification|\n");
        sb.Append("|--|---------------|-------------|\n");

        foreach (var suppression in diagnosticSuppressors.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedSuppressions).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id, StringComparer.Ordinal))
        {
            sb.Append("|`")
              .Append(suppression.Id)
              .Append("`|");

            if (suppression.SuppressedDiagnosticId.StartsWith("CA", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append('[')
                  .Append(suppression.SuppressedDiagnosticId)
                  .Append("](")
                  .Append($"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/").Append(suppression.SuppressedDiagnosticId.ToLowerInvariant()).Append("?WT.mc_id=DT-MVP-5003978")
                  .Append(')');
            }
            else
            {
                sb.Append('`').Append(suppression.SuppressedDiagnosticId).Append('`');
            }

            sb.Append('|')
              .Append(EscapeMarkdown(suppression.Justification.ToString(CultureInfo.InvariantCulture)))
              .Append('|')
              .Append('\n');
        }

        return sb.ToString();
    }

    private static void GenerateEditorConfig(StringBuilder sb, List<DiagnosticAnalyzer> analyzers, string? overrideSeverity = null)
    {
        sb.Append("```editorconfig\n");
        var first = true;
        foreach (var diagnostic in analyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id))
        {
            if (!first)
            {
                sb.Append('\n');
            }

            var severity = overrideSeverity;
            if (severity is null)
            {
                if (diagnostic.IsEnabledByDefault)
                {
                    severity = diagnostic.DefaultSeverity switch
                    {
                        DiagnosticSeverity.Hidden => "silent",
                        DiagnosticSeverity.Info => "suggestion",
                        DiagnosticSeverity.Warning => "warning",
                        DiagnosticSeverity.Error => "error",
                        _ => throw new InvalidOperationException($"{diagnostic.DefaultSeverity} not supported"),
                    };
                }
                else
                {
                    severity = "none";
                }
            }

            sb.Append("# ").Append(diagnostic.Id).Append(": ").Append(diagnostic.Title).Append('\n')
              .Append("dotnet_diagnostic.").Append(diagnostic.Id).Append(".severity = ").Append(severity).Append('\n');

            first = false;
        }

        sb.Append("```\n");
    }

    private static string GetSeverity(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Hidden => "👻",
            DiagnosticSeverity.Info => "ℹ️",
            DiagnosticSeverity.Warning => "⚠️",
            DiagnosticSeverity.Error => "❌",
            _ => throw new ArgumentOutOfRangeException(nameof(severity)),
        };
    }

    private static string EscapeMarkdown(string text)
    {
        return text
            .Replace("[", "\\[", StringComparison.Ordinal)
            .Replace("]", "\\]", StringComparison.Ordinal)
            .Replace("<", "\\<", StringComparison.Ordinal)
            .Replace(">", "\\>", StringComparison.Ordinal);
    }

    private static string GetBoolean(bool value)
    {
        return value ? "✔️" : "❌";
    }
}
