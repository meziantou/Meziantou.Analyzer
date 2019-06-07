using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DocumentationGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
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
            sb.AppendLine("|Id|Category|Description|Severity|Is enabled|Code fix|");
            sb.AppendLine("|--|--------|-----------|:------:|:--------:|:------:|");

            foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).OrderBy(diag => diag.Id))
            {
                var hasCodeFix = codeFixProviders.Any(codeFixProvider => codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id));
                sb.AppendLine($"|[{diagnostic.Id}](Rules/{diagnostic.Id})|{diagnostic.Category}|{diagnostic.Title}|{diagnostic.DefaultSeverity}|{diagnostic.IsEnabledByDefault}|{hasCodeFix}|");
            }

            Console.WriteLine(sb.ToString());
            var path = Path.GetFullPath(@"..\..\..\..\..\docs\README.md");
            Console.WriteLine(path);
            File.WriteAllText(path, sb.ToString());
        }
    }
}
