using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Meziantou.Analyzer.Configurations;

namespace Meziantou.Analyzer.Rules;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FileNameMustMatchTypeNameAnalyzer : DiagnosticAnalyzer
{
    private enum TypeNameMatchMode
    {
        Exact,
        Prefix,
        LongestCommonPrefix,
    }

    private static readonly DiagnosticDescriptor Rule = new(
        RuleIdentifiers.FileNameMustMatchTypeName,
        title: "File name must match type name",
        messageFormat: "File name must match type name ({0} {1}), expected file name: {2}",
        RuleCategories.Design,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.FileNameMustMatchTypeName));

    private static readonly ConfigurationDefinition<string> ExcludedSymbolNamesConfiguration = new("dotnet_diagnostic." + Rule.Id + ".excluded_symbol_names", defaultValue: string.Empty);
    private static readonly ConfigurationDefinition<bool> ExcludeFileLocalTypesConfiguration = new(Rule.Id + ".exclude_file_local_types", defaultValue: true);
    private static readonly ConfigurationDefinition<bool> OnlyValidateFirstTypeConfiguration = new(Rule.Id + ".only_validate_first_type", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> AllowOfTForAllGenericTypesConfiguration = new(Rule.Id + ".allow_oft_for_all_generic_types", defaultValue: false);
    private static readonly ConfigurationDefinition<string> ModeConfiguration = new(Rule.Id + ".mode", defaultValue: string.Empty);
    private static readonly ConfigurationDefinition<bool> AllowTypeNamePrefixConfiguration = new(Rule.Id + ".allow_type_name_prefix", defaultValue: false);
    private static readonly ConfigurationDefinition<bool> UseLongestTypeNamePrefixConfiguration = new(Rule.Id + ".use_longest_type_name_prefix", defaultValue: false);
    private static readonly ConfigurationDefinition<string> ExcludedFileNamePartsConfiguration = new(Rule.Id + ".excluded_file_name_parts", defaultValue: string.Empty);
    private static readonly ConfigurationDefinition<string> ExcludedFileNamePartsRegexConfiguration = new(Rule.Id + ".excluded_file_name_parts_regex", defaultValue: string.Empty) { RegexOptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

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

            if (symbol.IsFileLocal && context.Options.GetConfigurationValue(location.SourceTree, ExcludeFileLocalTypesConfiguration))
                continue;

            var symbolName = symbol.Name;

            // dotnet_diagnostic.MA0048.excluded_symbol_names
            var excludedSymbolNames = context.Options.GetConfigurationValue(location.SourceTree, ExcludedSymbolNamesConfiguration);
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
            if (context.Options.GetConfigurationValue(location.SourceTree, OnlyValidateFirstTypeConfiguration))
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
            if (IsMatchingFileName(context, symbol, location.SourceTree, GetFileName(filePath.AsSpan()), typeNameMatchMode))
                continue;

            // MA0048.excluded_file_name_parts
            var fileNameWithoutExcludedParts = GetFileNameWithoutExcludedParts(context, location.SourceTree, filePath.AsSpan());
            if (fileNameWithoutExcludedParts is not null && IsMatchingFileName(context, symbol, location.SourceTree, fileNameWithoutExcludedParts.AsSpan(), typeNameMatchMode))
                continue;

            context.ReportDiagnostic(Rule, location, GetTypeKindDisplayString(symbol), symbolName, GetExpectedFileName(context, symbol, location.SourceTree, typeNameMatchMode));
        }
    }

    private static bool IsMatchingFileName(SymbolAnalysisContext context, INamedTypeSymbol symbol, SyntaxTree sourceTree, ReadOnlySpan<char> fileName, TypeNameMatchMode typeNameMatchMode)
    {
        var symbolName = symbol.Name;

        if (fileName.Equals(symbolName.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return true;

        if (!fileName.IsEmpty && symbolName.AsSpan().StartsWith(fileName, StringComparison.OrdinalIgnoreCase) &&
            (typeNameMatchMode is TypeNameMatchMode.Prefix ||
            (typeNameMatchMode is TypeNameMatchMode.LongestCommonPrefix && IsLongestTypeNamePrefix(context, sourceTree, fileName))))
            return true;

        if (symbol.Arity > 0)
        {
            // Type`1
            if (fileName.Equals((symbolName + "`" + symbol.Arity.ToString(CultureInfo.InvariantCulture)).AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;

            // Type{T}
            if (fileName.Equals((symbolName + '{' + string.Join(',', symbol.TypeParameters.Select(t => t.Name)) + '}').AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        if (symbol.Arity == 1 || (symbol.Arity > 1 && context.Options.GetConfigurationValue(sourceTree, AllowOfTForAllGenericTypesConfiguration)))
        {
            // TypeOfT
            if (fileName.Equals((symbolName + "OfT").AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static ReadOnlySpan<char> RemoveDirectory(ReadOnlySpan<char> filePath)
    {
        var fileNameIndex = filePath.LastIndexOfAny('/', '\\');
        if (fileNameIndex > 0)
            return filePath[(fileNameIndex + 1)..];

        return filePath;
    }

    private static ReadOnlySpan<char> GetFileName(ReadOnlySpan<char> filePath)
    {
        var fileName = RemoveDirectory(filePath);

        var index = fileName.IndexOf('.');
        if (index < 0)
            return fileName;

        return fileName[..index];
    }

    private static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> filePath)
    {
        var fileName = RemoveDirectory(filePath);

        var index = fileName.LastIndexOf('.');
        if (index < 0)
            return fileName;

        return fileName[..index];
    }

    private static string? GetFileNameWithoutExcludedParts(SymbolAnalysisContext context, SyntaxTree sourceTree, ReadOnlySpan<char> filePath)
    {
        var excludedParts = context.Options.GetConfigurationValue(sourceTree, ExcludedFileNamePartsConfiguration);
        var excludedPartsRegex = context.Options.GetConfigurationValue(sourceTree, ExcludedFileNamePartsRegexConfiguration);
        if (string.IsNullOrEmpty(excludedParts) && string.IsNullOrEmpty(excludedPartsRegex))
            return null;

        var fileName = GetFileNameWithoutExtension(filePath).ToString();

        // The regex is applied first, so it can match the dots that MA0048.excluded_file_name_parts may remove
        if (!string.IsNullOrEmpty(excludedPartsRegex))
        {
            fileName = RegexCache.Replace(excludedPartsRegex, ExcludedFileNamePartsRegexConfiguration.RegexOptions, fileName, replacement: "", defaultValue: fileName);
        }

        foreach (var part in excludedParts.Split([',', '|'], StringSplitOptions.RemoveEmptyEntries))
        {
            var excludedPart = part.Trim();
            if (excludedPart.Length is 0)
                continue;

            fileName = fileName.Replace(excludedPart, "", StringComparison.OrdinalIgnoreCase);
        }

        return fileName.Length is 0 ? null : fileName;
    }

    private static TypeNameMatchMode GetTypeNameMatchMode(SymbolAnalysisContext context, SyntaxTree sourceTree)
    {
        var mode = context.Options.GetConfigurationValue(sourceTree, ModeConfiguration);
        if (mode.Equals(nameof(TypeNameMatchMode.Exact), StringComparison.OrdinalIgnoreCase))
            return TypeNameMatchMode.Exact;

        if (mode.Equals(nameof(TypeNameMatchMode.Prefix), StringComparison.OrdinalIgnoreCase))
            return TypeNameMatchMode.Prefix;

        if (mode.Equals(nameof(TypeNameMatchMode.LongestCommonPrefix), StringComparison.OrdinalIgnoreCase))
            return TypeNameMatchMode.LongestCommonPrefix;

        // Backward compatibility
        if (!context.Options.GetConfigurationValue(sourceTree, AllowTypeNamePrefixConfiguration))
            return TypeNameMatchMode.Exact;

        return context.Options.GetConfigurationValue(sourceTree, UseLongestTypeNamePrefixConfiguration)
            ? TypeNameMatchMode.LongestCommonPrefix
            : TypeNameMatchMode.Prefix;
    }

    private static string GetExpectedFileName(SymbolAnalysisContext context, INamedTypeSymbol symbol, SyntaxTree sourceTree, TypeNameMatchMode typeNameMatchMode)
    {
        return typeNameMatchMode switch
        {
            TypeNameMatchMode.Exact => "'" + symbol.Name + "'",
            TypeNameMatchMode.Prefix => "a prefix of '" + symbol.Name + "'",
            TypeNameMatchMode.LongestCommonPrefix => GetExpectedLongestCommonPrefixFileName(context, sourceTree, symbol.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(typeNameMatchMode)),
        };
    }

    private static string GetExpectedLongestCommonPrefixFileName(SymbolAnalysisContext context, SyntaxTree sourceTree, string fallbackTypeName)
    {
        var typeNames = GetTopLevelTypeNames(context, sourceTree);
        var longestCommonPrefixLength = GetLongestCommonPrefixLength(typeNames);
        if (longestCommonPrefixLength <= 0)
            return "'" + fallbackTypeName + "'";

        return "'" + typeNames![0].AsSpan(0, longestCommonPrefixLength).ToString() + "'";
    }

    private static bool IsLongestTypeNamePrefix(SymbolAnalysisContext context, SyntaxTree sourceTree, ReadOnlySpan<char> fileName)
    {
        var typeNames = GetTopLevelTypeNames(context, sourceTree);
        var longestCommonPrefixLength = GetLongestCommonPrefixLength(typeNames);
        if (longestCommonPrefixLength < 0)
            return true;

        if (longestCommonPrefixLength == 0)
            return false;

        return longestCommonPrefixLength == fileName.Length &&
               typeNames![0].AsSpan(0, longestCommonPrefixLength).Equals(fileName, StringComparison.OrdinalIgnoreCase);
    }

    private static List<string>? GetTopLevelTypeNames(SymbolAnalysisContext context, SyntaxTree sourceTree)
    {
        var root = sourceTree.GetRoot(context.CancellationToken);
        List<string>? typeNames = null;

        var excludeFileLocalTypes = context.Options.GetConfigurationValue(sourceTree, ExcludeFileLocalTypesConfiguration);

        foreach (var node in root.DescendantNodesAndSelf(descendIntoChildren: static node => !IsTypeDeclaration(node)))
        {
            if (!TryGetTypeDeclarationName(node, out var typeName))
                continue;

            if (excludeFileLocalTypes && IsFileLocalType(node))
                continue;

            typeNames ??= new List<string>();
            typeNames.Add(typeName);
        }

        return typeNames;
    }

    // -1: no/one type, 0: no common prefix, >0: prefix length
    private static int GetLongestCommonPrefixLength(List<string>? typeNames)
    {
        if (typeNames is null || typeNames.Count <= 1)
            return -1;

        var commonPrefixLength = typeNames[0].Length;
        for (var i = 1; i < typeNames.Count; i++)
        {
            commonPrefixLength = GetCommonPrefixLength(typeNames[0], typeNames[i], commonPrefixLength);
            if (commonPrefixLength == 0)
                return 0;
        }

        return commonPrefixLength;
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

    private static bool IsFileLocalType(SyntaxNode node)
    {
        return node switch
        {
            BaseTypeDeclarationSyntax typeDeclaration => typeDeclaration.Modifiers.Any(SyntaxKind.FileKeyword),
            DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.Modifiers.Any(SyntaxKind.FileKeyword),
            _ => false,
        };
    }

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
            TypeKind.Class when symbol.IsRecord => "record",
            TypeKind.Class => "class",
            TypeKind.Struct when symbol.IsRecord => "record struct",
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
