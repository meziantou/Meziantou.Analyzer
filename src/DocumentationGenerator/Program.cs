﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DocumentationGenerator
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("You must specify the output folder");
                return;
            }

            var outputFolder = args[0];

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
                sb.AppendLine($"|[{diagnostic.Id}](Rules/{diagnostic.Id}.md)|{diagnostic.Category}|{diagnostic.Title}|<span title='{diagnostic.DefaultSeverity}'>{GetSeverity(diagnostic.DefaultSeverity)}</span>|{GetBoolean(diagnostic.IsEnabledByDefault)}|{GetBoolean(hasCodeFix)}|");
            }

            Console.WriteLine(sb.ToString());
            var path = Path.GetFullPath(Path.Combine(args[0], @"README.md"));
            Console.WriteLine(path);
            File.WriteAllText(path, sb.ToString());
        }

        private static string GetSeverity(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    return "👻";
                case DiagnosticSeverity.Info:
                    return "ℹ️";
                case DiagnosticSeverity.Warning:
                    return "⚠️";
                case DiagnosticSeverity.Error:
                    return "❌";
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity));
            }
        }

        private static string GetBoolean(bool value)
        {
            return value ? "✔️" : "❌";
        }
    }
}
