using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Meziantou.Analyzer.Configurations;
using Meziantou.Analyzer.Internals;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FileNameMustMatchTypeNameAnalyzer : DiagnosticAnalyzer
{
    private enum TypeNameMatchMode
    {
        Exact,
        Prefix,
        LongestPrefix,
    }

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.FileNameMustMatchTypeName,
        title: "File name must match type name",
        messageFormat: "File name must match type name ({0} {1})",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FileNameMustMatchTypeName));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
            if (symbol.ContainingType is not null)
                continue;

            var typeNameMatchMode = GetTypeNameMatchMode(context, location.SourceTree);

#if ROSLYN_4_4_OR_GREATER
            if (symbol.IsFileLocal && context.Options.GetConfigurationValue(location.SourceTree, Rule.Id + ".exclude_file_local_types", defaultValue: true))
                continue;
#endif

            var symbolName = symbol.Name;

            // dotnet_diagnostic.MA0048.excluded_symbol_names
            var excludedSymbolNames = context.Options.GetConfigurationValue(location.SourceTree, "dotnet_diagnostic." + Rule.Id + ".excluded_symbol_names", defaultValue: string.Empty);
            if (!string.IsNullOrEmpty(excludedSymbolNames))
            {
                var symbolDeclarationId = DocumentationCommentId.CreateDeclarationId(symbol);
                var excludedSymbolNamesSplit = excludedSymbolNames.Split('|', StringSplitOptions.RemoveEmptyEntries);
                var matched = false;

                foreach (var excludedSymbolName in excludedSymbolNamesSplit)
                {
                    if (IsWildcardMatch(symbolName, excludedSymbolName) || (symbolDeclarationId is not null && IsWildcardMatch(symbolDeclarationId, excludedSymbolName)))
                        matched = true;
                }

                // to continue the outer foreach loop
                if (matched)
                    continue;
            }

            // MA0048.only_validate_first_type
            if (context.Options.GetConfigurationValue(location.SourceTree, Rule.Id + ".only_validate_first_type", defaultValue: false))
            {
                var root = location.SourceTree.GetRoot(context.CancellationToken);
                var symbolNode = root.FindNode(location.SourceSpan);

                static bool IsTypeDeclaration(SyntaxNode syntaxNode) => syntaxNode is BaseTypeDeclarationSyntax;

                var isFirstType = true;
                foreach (var node in root.DescendantNodesAndSelf(descendIntoChildren: node => !IsTypeDeclaration(node)))
                {
                    if (!IsTypeDeclaration(node))
                        continue;

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
            var fileName = filePath is not null ? GetFileName(filePath.AsSpan()) : null;

            if (fileName.Equals(symbolName.AsSpan(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (!fileName.IsEmpty && symbolName.AsSpan().StartsWith(fileName, StringComparison.OrdinalIgnoreCase) &&
                (typeNameMatchMode is TypeNameMatchMode.Prefix ||
                (typeNameMatchMode is TypeNameMatchMode.LongestPrefix && IsLongestTypeNamePrefix(context, location.SourceTree, fileName))))
                continue;

            if (symbol.Arity > 0)
            {
                // Type`1
                if (fileName.Equals((symbolName + "`" + symbol.Arity.ToString(CultureInfo.InvariantCulture)).AsSpan(), StringComparison.OrdinalIgnoreCase))
                    continue;

                // Type{T}
                if (fileName.Equals((symbolName + '{' + string.Join(',', symbol.TypeParameters.Select(t => t.Name)) + '}').AsSpan(), StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            if (symbol.Arity == 1 || (symbol.Arity > 1 && context.Options.GetConfigurationValue(location.SourceTree, Rule.Id + ".allow_oft_for_all_generic_types", defaultValue: false)))
            {
                // TypeOfT
                if (fileName.Equals((symbolName + "OfT").AsSpan(), StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            context.ReportDiagnostic(Rule, location, GetTypeKindDisplayString(symbol), symbolName);
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

    private static TypeNameMatchMode GetTypeNameMatchMode(SymbolAnalysisContext context, SyntaxTree sourceTree)
    {
        var mode = context.Options.GetConfigurationValue(sourceTree, Rule.Id + ".mode", defaultValue: string.Empty);
        if (mode.Equals(nameof(TypeNameMatchMode.Exact), StringComparison.OrdinalIgnoreCase))
            return TypeNameMatchMode.Exact;

        if (mode.Equals(nameof(TypeNameMatchMode.Prefix), StringComparison.OrdinalIgnoreCase))
            return TypeNameMatchMode.Prefix;

        if (mode.Equals(nameof(TypeNameMatchMode.LongestPrefix), StringComparison.OrdinalIgnoreCase))
            return TypeNameMatchMode.LongestPrefix;

        // Backward compatibility
        if (!context.Options.GetConfigurationValue(sourceTree, Rule.Id + ".allow_type_name_prefix", defaultValue: false))
            return TypeNameMatchMode.Exact;

        return context.Options.GetConfigurationValue(sourceTree, Rule.Id + ".use_longest_type_name_prefix", defaultValue: false)
            ? TypeNameMatchMode.LongestPrefix
            : TypeNameMatchMode.Prefix;
    }

    private static bool IsLongestTypeNamePrefix(SymbolAnalysisContext context, SyntaxTree sourceTree, ReadOnlySpan<char> fileName)
    {
        var root = sourceTree.GetRoot(context.CancellationToken);
        List<string>? typeNames = null;

#if ROSLYN_4_4_OR_GREATER
        var excludeFileLocalTypes = context.Options.GetConfigurationValue(sourceTree, Rule.Id + ".exclude_file_local_types", defaultValue: true);
#endif

        foreach (var node in root.DescendantNodesAndSelf(descendIntoChildren: static node => !IsTypeDeclaration(node)))
        {
            if (!TryGetTypeDeclarationName(node, out var typeName))
                continue;

#if ROSLYN_4_4_OR_GREATER
            if (excludeFileLocalTypes && IsFileLocalType(node))
                continue;
#endif

            typeNames ??= new List<string>();
            typeNames.Add(typeName);
        }

        if (typeNames is null || typeNames.Count <= 1)
            return true;

        var commonPrefixLength = typeNames[0].Length;
        for (var i = 1; i < typeNames.Count; i++)
        {
            commonPrefixLength = GetCommonPrefixLength(typeNames[0], typeNames[i], commonPrefixLength);
            if (commonPrefixLength == 0)
                return false;
        }

        return commonPrefixLength == fileName.Length &&
               typeNames[0].AsSpan(0, commonPrefixLength).Equals(fileName, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetCommonPrefixLength(string left, string right, int maxLength)
    {
        var length = Math.Min(maxLength, right.Length);
        var index = 0;
        while (index < length && char.ToUpperInvariant(left[index]) == char.ToUpperInvariant(right[index]))
        {
            index++;
        }

        return index;
    }

    private static bool IsTypeDeclaration(SyntaxNode node)
    {
        return node is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax;
    }

    private static bool TryGetTypeDeclarationName(SyntaxNode node, [NotNullWhen(true)] out string? typeName)
    {
        switch (node)
        {
            case BaseTypeDeclarationSyntax typeDeclaration:
                typeName = typeDeclaration.Identifier.ValueText;
                return true;
            case DelegateDeclarationSyntax delegateDeclaration:
                typeName = delegateDeclaration.Identifier.ValueText;
                return true;
            default:
                typeName = null;
                return false;
        }
    }

#if ROSLYN_4_4_OR_GREATER
    private static bool IsFileLocalType(SyntaxNode node)
    {
        return node switch
        {
            BaseTypeDeclarationSyntax typeDeclaration => typeDeclaration.Modifiers.Any(SyntaxKind.FileKeyword),
            DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.Modifiers.Any(SyntaxKind.FileKeyword),
            _ => false,
        };
    }
#endif

    /// <summary>
    /// Implemented wildcard pattern match
    /// </summary>
    /// <example>
    /// Would match FooManager for expression *Manager
    /// </example>
    private static bool IsWildcardMatch(string input, string pattern)
    {
        var wildcardPattern = $"^{Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal)}$";
        return Regex.IsMatch(input, wildcardPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
    }

    private static string GetTypeKindDisplayString(INamedTypeSymbol symbol)
    {
        return symbol.TypeKind switch
        {
#if CSHARP10_OR_GREATER
            TypeKind.Class when symbol.IsRecord => "record",
#endif
            TypeKind.Class => "class",
#if CSHARP10_OR_GREATER
            TypeKind.Struct when symbol.IsRecord => "record struct",
#endif
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            TypeKind.Enum => "enum",
            TypeKind.Delegate => "delegate",
#pragma warning disable CA1308 // Normalize strings to uppercase
            _ => symbol.TypeKind.ToString().ToLowerInvariant(),
#pragma warning restore CA1308 // Normalize strings to uppercase
        };
    }
}
