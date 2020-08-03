using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer.Rules
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FileNameMustMatchTypeNameAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            RuleIdentifiers.FileNameMustMatchTypeName,
            title: "File name must match type name",
            messageFormat: "File name must match type name",
            RuleCategories.Design,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FileNameMustMatchTypeName));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.IsImplicitlyDeclared || symbol.IsImplicitClass || symbol.Name.Contains("$"))
                return;

            foreach (var location in symbol.Locations)
            {
                if (!location.IsInSource || string.IsNullOrEmpty(location.SourceTree?.FilePath))
                    continue;

                // Nested type
                if (symbol.ContainingType != null)
                    continue;

                var filePath = location.SourceTree?.FilePath;
                var fileName = filePath == null ? null : GetFileName(filePath);
                var symbolName = symbol.Name;
                if (string.Equals(fileName, symbolName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (symbol.Arity > 0)
                {
                    // Type`1
                    if (string.Equals(fileName, symbolName + "`" + symbol.Arity.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (symbol.Arity == 1)
                {
                    // TypeOfT
                    if (string.Equals(fileName, symbolName + "OfT", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                context.ReportDiagnostic(s_rule, location);
            }
        }

        private static string GetFileName(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            var index = fileName.IndexOf('.');
            if (index < 0)
                return fileName;

            return fileName.Substring(0, index);
        }
    }
}
