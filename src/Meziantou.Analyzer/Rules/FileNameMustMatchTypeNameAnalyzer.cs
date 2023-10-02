﻿using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FileNameMustMatchTypeNameAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_rule = new(
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
        if (symbol.IsImplicitlyDeclared || symbol.IsImplicitClass || symbol.Name.Contains('$', StringComparison.Ordinal))
            return;

        foreach (var location in symbol.Locations)
        {
            if (!location.IsInSource || string.IsNullOrEmpty(location.SourceTree?.FilePath))
                continue;

            // Nested type
            if (symbol.ContainingType != null)
                continue;

#if ROSLYN_4_4_OR_GREATER
            if (symbol.IsFileLocal && context.Options.GetConfigurationValue(location.SourceTree, s_rule.Id + ".exclude_file_local_types", defaultValue: true))
                continue;
#endif

            if (context.Options.GetConfigurationValue(location.SourceTree, s_rule.Id + ".only_validate_first_type", defaultValue: false))
            {
                var root = location.SourceTree.GetRoot(context.CancellationToken);
                var symbolNode = root.FindNode(location.SourceSpan);

                static bool IsTypeDeclaration(SyntaxNode syntaxNode) => syntaxNode is BaseTypeDeclarationSyntax;

                var isFirstType = true;
                foreach (var node in root.DescendantNodesAndSelf(descendIntoChildren: node => !IsTypeDeclaration(node)))
                {
                    if (node.SpanStart < symbolNode.SpanStart)
                    {
                        isFirstType = false;
                        break;
                    }
                }

                if (!isFirstType)
                    continue;
            }

            var filePath = location.SourceTree.FilePath;
            var fileName = filePath == null ? null : GetFileName(filePath.AsSpan());
            var symbolName = symbol.Name;
            if (fileName.Equals(symbolName.AsSpan(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (symbol.Arity > 0)
            {
                // Type`1
                if (fileName.Equals((symbolName + "`" + symbol.Arity.ToString(CultureInfo.InvariantCulture)).AsSpan(), StringComparison.OrdinalIgnoreCase))
                    continue;

                // Type{T}
                if (fileName.Equals((symbolName + '{' + string.Join(",", symbol.TypeParameters.Select(t => t.Name)) + '}').AsSpan(), StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            if (symbol.Arity == 1)
            {
                // TypeOfT
                if (fileName.Equals((symbolName + "OfT").AsSpan(), StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            context.ReportDiagnostic(s_rule, location);
        }
    }

    private static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> filePath)
    {
        var fileNameIndex = filePath.LastIndexOfAny('/', '\\');
        if (fileNameIndex > 0)
        {
            filePath = filePath[(fileNameIndex + 1)..];
        }

        var index = filePath.IndexOf('.');
        if (index < 0)
            return filePath;

        return filePath[..index];
    }
}
