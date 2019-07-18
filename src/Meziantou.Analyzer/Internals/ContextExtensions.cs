using System;
using System.Collections.Immutable;
using System.Linq;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer
{
    internal static class ContextExtensions
    {
        public static bool IsEnabled(this AnalyzerOptions options, DiagnosticDescriptor descriptor, string filePath)
        {
            var severity = GetSeverity(options, descriptor, filePath);
            return severity != Microsoft.CodeAnalysis.ReportDiagnostic.Suppress;
        }

        public static ReportDiagnostic? GetSeverity(this AnalyzerOptions options, DiagnosticDescriptor descriptor, string filePath)
        {
            return options.GetConfigurationValue(filePath, descriptor.Id + ".severity", defaultValue: default(ReportDiagnostic?));
        }

        private static Diagnostic CreateDiagnostic(AnalyzerOptions options, DiagnosticDescriptor descriptor, Location location, ImmutableDictionary<string, string> properties, params string[] messageArgs)
        {
            var severity = GetSeverity(options, descriptor, location.SourceTree.FilePath);
            if (severity == null || severity.Value == Microsoft.CodeAnalysis.ReportDiagnostic.Default)
            {
                return Diagnostic.Create(descriptor, location, properties, messageArgs);
            }
            else
            {
                var diagnosticSeverity = severity.Value switch
                {
                    Microsoft.CodeAnalysis.ReportDiagnostic.Info => DiagnosticSeverity.Info,
                    Microsoft.CodeAnalysis.ReportDiagnostic.Warn => DiagnosticSeverity.Warning,
                    Microsoft.CodeAnalysis.ReportDiagnostic.Error => DiagnosticSeverity.Error,
                    Microsoft.CodeAnalysis.ReportDiagnostic.Hidden => DiagnosticSeverity.Hidden,
                    _ => throw new InvalidOperationException($"{severity.Value} is not valid."),
                };

                return Diagnostic.Create(descriptor, location, diagnosticSeverity, Enumerable.Empty<Location>(), properties, messageArgs);
            }
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxToken syntaxToken, params string[] messageArgs)
        {
            ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string>.Empty, syntaxToken, messageArgs);
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, SyntaxToken syntaxToken, params string[] messageArgs)
        {
            if (IsEnabled(context.Options, descriptor, syntaxToken.SyntaxTree.FilePath))
            {
                context.ReportDiagnostic(CreateDiagnostic(context.Options, descriptor, syntaxToken.GetLocation(), properties, messageArgs));
            }
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params string[] messageArgs)
        {
            ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string>.Empty, syntaxNode, messageArgs);
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, SyntaxNode syntaxNode, params string[] messageArgs)
        {
            if (IsEnabled(context.Options, descriptor, syntaxNode.SyntaxTree.FilePath))
            {
                context.ReportDiagnostic(CreateDiagnostic(context.Options, descriptor, syntaxNode.GetLocation(), properties, messageArgs));
            }
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params string[] messageArgs)
        {
            ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string>.Empty, symbol, messageArgs);
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, ISymbol symbol, params string[] messageArgs)
        {
            foreach (var location in symbol.Locations)
            {
                if (IsEnabled(context.Options, descriptor, location.SourceTree.FilePath))
                {
                    context.ReportDiagnostic(CreateDiagnostic(context.Options, descriptor, location, properties, messageArgs));
                }
            }
        }

        public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params string[] messageArgs)
        {
            ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string>.Empty, symbol, messageArgs);
        }

        public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, ISymbol symbol, params string[] messageArgs)
        {
            foreach (var location in symbol.Locations)
            {
                ReportDiagnostic(context, descriptor, properties, location, messageArgs);
            }
        }

        public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, Location location, params string[] messageArgs)
        {
            context.ReportDiagnostic(Diagnostic.Create(descriptor, location, ImmutableDictionary<string, string>.Empty, messageArgs));
        }

        public static void ReportDiagnostic(this SymbolAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, Location location, params string[] messageArgs)
        {
            if (IsEnabled(context.Options, descriptor, location.SourceTree.FilePath))
            {
                context.ReportDiagnostic(CreateDiagnostic(context.Options, descriptor, location, properties, messageArgs));
            }
        }

        public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, IOperation operation, params string[] messageArgs)
        {
            ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string>.Empty, operation, messageArgs);
        }

        public static void ReportDiagnostic(this OperationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, IOperation operation, params string[] messageArgs)
        {
            if (IsEnabled(context.Options, descriptor, operation.Syntax.SyntaxTree.FilePath))
            {
                context.ReportDiagnostic(CreateDiagnostic(context.Options, descriptor, operation.Syntax.GetLocation(), properties, messageArgs));
            }
        }

        public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params string[] messageArgs)
        {
            ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string>.Empty, symbol, messageArgs);
        }

        public static void ReportDiagnostic(this CompilationAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, ISymbol symbol, params string[] messageArgs)
        {
            foreach (var location in symbol.Locations)
            {
                if (IsEnabled(context.Options, descriptor, location.SourceTree.FilePath))
                {
                    context.ReportDiagnostic(CreateDiagnostic(context.Options, descriptor, location, properties, messageArgs));
                }
            }
        }

        public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ISymbol symbol, params string[] messageArgs)
        {
            ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string>.Empty, symbol, messageArgs);
        }

        public static void ReportDiagnostic(this OperationBlockAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, ISymbol symbol, params string[] messageArgs)
        {
            foreach (var location in symbol.Locations)
            {
                if (IsEnabled(context.Options, descriptor, location.SourceTree.FilePath))
                {
                    context.ReportDiagnostic(CreateDiagnostic(context.Options, descriptor, location, properties, messageArgs));
                }
            }
        }
    }
}
