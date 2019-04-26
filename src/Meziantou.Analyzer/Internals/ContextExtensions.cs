﻿using System.Collections.Immutable;
using Meziantou.Analyzer.Configurations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Meziantou.Analyzer
{
    internal static class ContextExtensions
    {
        public static bool IsEnabled(this AnalyzerOptions options, DiagnosticDescriptor descriptor, string filePath)
        {
            if (options.TryGetConfigurationValue(filePath, "meziantou." + descriptor.Id + ".enabled", out var value))
            {
                if (bool.TryParse(value, out var isEnabled) && !isEnabled)
                    return false;
            }

            return true;
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, SyntaxNode syntaxNode, params string[] messageArgs)
        {
            ReportDiagnostic(context, descriptor, ImmutableDictionary<string, string>.Empty, syntaxNode, messageArgs);
        }

        public static void ReportDiagnostic(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor descriptor, ImmutableDictionary<string, string> properties, SyntaxNode syntaxNode, params string[] messageArgs)
        {
            if (IsEnabled(context.Options, descriptor, syntaxNode.SyntaxTree.FilePath))
            {
                context.ReportDiagnostic(Diagnostic.Create(descriptor, syntaxNode.GetLocation(), properties, messageArgs));
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
                    context.ReportDiagnostic(Diagnostic.Create(descriptor, location, properties, messageArgs));
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
                context.ReportDiagnostic(Diagnostic.Create(descriptor, location, properties, messageArgs));
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
                context.ReportDiagnostic(Diagnostic.Create(descriptor, operation.Syntax.GetLocation(), properties, messageArgs));
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
                    context.ReportDiagnostic(Diagnostic.Create(descriptor, location, properties, messageArgs));
                }
            }
        }
    }
}
