using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DocumentationGenerator
{
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

            if (args.Length > 1)
            {
                var assemblyPath = Path.GetFullPath(args[0]);
            }

            var assembly = typeof(Meziantou.Analyzer.Rules.CommaFixer).Assembly;
            var diagnosticAnalyzers = assembly.GetExportedTypes()
                .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
                .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type))
                .ToList();

            var codeFixProviders = assembly.GetExportedTypes()
                .Where(type => !type.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(type))
                .Select(type => (CodeFixProvider)Activator.CreateInstance(type))
                .ToList();

            var sb = new StringBuilder();
            sb.Append("# ").Append(assembly.GetName().Name).Append("'s rules");
            sb.AppendLine();
            sb.AppendLine("|Id|Category|Description|Severity|Is enabled|Code fix|");
            sb.AppendLine("|--|--------|-----------|:------:|:--------:|:------:|");

            foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).OrderBy(diag => diag.Id, StringComparer.Ordinal))
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
                  .Append("|<span title='")
                  .Append(HtmlEncoder.Default.Encode(diagnostic.DefaultSeverity.ToString()))
                  .Append("'>")
                  .Append(GetSeverity(diagnostic.DefaultSeverity))
                  .Append("</span>|")
                  .Append(GetBoolean(diagnostic.IsEnabledByDefault))
                  .Append('|')
                  .Append(GetBoolean(hasCodeFix))
                  .Append('|')
                  .AppendLine();
            }

            sb.AppendLine();

            sb.AppendLine("# .editorconfig - default values");
            sb.AppendLine();
            sb.AppendLine("```editorconfig");
            foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).OrderBy(diag => diag.Id))
            {
                var severity = diagnostic.IsEnabledByDefault ?
                                    diagnostic.DefaultSeverity switch
                                    {
                                        DiagnosticSeverity.Hidden => "silent    ",
                                        DiagnosticSeverity.Info => "suggestion",
                                        DiagnosticSeverity.Warning => "warning   ",
                                        DiagnosticSeverity.Error => "error     ",
                                        _ => throw new InvalidOperationException($"{diagnostic.DefaultSeverity} not supported"),
                                    }
                                    : "none      ";

                sb.Append("dotnet_diagnostic.").Append(diagnostic.Id).Append(".severity = ").Append(severity)
                    .Append(" # ").Append(diagnostic.Id).Append(": ").Append(diagnostic.Title)
                    .AppendLine();
            }
            sb.AppendLine("```");
            sb.AppendLine();

            sb.AppendLine("# .editorconfig - all rules disabled");
            sb.AppendLine();
            sb.AppendLine("```editorconfig");
            foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).OrderBy(diag => diag.Id))
            {
                sb.Append("dotnet_diagnostic.").Append(diagnostic.Id).Append(".severity = none      ")
                    .Append(" # ").Append(diagnostic.Id).Append(": ").Append(diagnostic.Title)
                    .AppendLine();
            }
            sb.AppendLine("```");

            Console.WriteLine(sb.ToString());
            var path = Path.GetFullPath(Path.Combine(outputFolder, @"README.md"));
            Console.WriteLine(path);
            File.WriteAllText(path, sb.ToString());

            // Write title in detail pages
            foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).OrderBy(diag => diag.Id))
            {
                var title = $"# {diagnostic.Id} - {EscapeMarkdown(diagnostic.Title.ToString(CultureInfo.InvariantCulture))}";
                var detailPath = Path.GetFullPath(Path.Combine(outputFolder, "Rules", diagnostic.Id + ".md"));
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
                .Replace("<", "\\<", StringComparison.Ordinal)
                .Replace(">", "\\>", StringComparison.Ordinal);
        }

        private static string GetBoolean(bool value)
        {
            return value ? "✔️" : "❌";
        }
    }
}
