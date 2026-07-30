#pragma warning disable RS1035
#pragma warning disable CA1849
#pragma warning disable MA0004
#pragma warning disable MA0009
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Meziantou.Analyzer.Configurations;
using Meziantou.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;

if (!FullPath.CurrentDirectory().TryFindGitRepositoryRoot(out var outputFolder))
{
    Console.WriteLine("Cannot find the current git folder");
    return 1;
}
var fileWritten = 0;
var documentationValidationErrorCount = 0;

var assemblies = new[] { typeof(Meziantou.Analyzer.Rules.CommaAnalyzer).Assembly, typeof(Meziantou.Analyzer.Rules.CommaFixer).Assembly };
var diagnosticAnalyzers = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
    .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
    .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
    .ToList();

var codeFixProviders = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
    .Where(type => !type.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(type))
    .Select(type => (CodeFixProvider)Activator.CreateInstance(type)!)
    .ToList();

var codeRefactoringProviders = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
    .Where(type => !type.IsAbstract && typeof(CodeRefactoringProvider).IsAssignableFrom(type))
    .Select(type => (CodeRefactoringProvider)Activator.CreateInstance(type)!)
    .ToList();

var diagnosticSuppressors = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
  .Where(type => !type.IsAbstract && typeof(DiagnosticSuppressor).IsAssignableFrom(type))
  .Select(type => (DiagnosticSuppressor)Activator.CreateInstance(type)!)
  .ToList();

var ruleConfigurationKeys = GetRuleConfigurationKeys(assemblies);

var sb = new StringBuilder();
sb.Append("# ").Append(assemblies[0].GetName().Name).Append("'s rules\n");
var rulesTable = GenerateRulesTable(diagnosticAnalyzers, codeFixProviders, ruleConfigurationKeys);
sb.Append(rulesTable);

var suppressorsTable = GenerateSuppressorsTable(diagnosticSuppressors);
sb.Append('\n');
sb.Append(suppressorsTable);

var refactoringsTable = GenerateRefactoringsTable(codeRefactoringProviders);
sb.Append("\n# Refactorings\n\n");
sb.Append(refactoringsTable);

Console.WriteLine(sb.ToString());

// Update home readme
{
    // The main readme is embedded into the NuGet package and rendered by nuget.org.
    // nuget.org's markdown support is limited. Raw html in table is not supported.
    var readmePath = outputFolder / "README.md";
    var readmeContent = await File.ReadAllTextAsync(readmePath);
    var newContent = Regex.Replace(readmeContent, "(?<=<!-- rules -->\\r?\\n).*(?=<!-- rules -->)", "\n" + GenerateRulesTable(diagnosticAnalyzers, codeFixProviders, ruleConfigurationKeys, addTitle: false) + "\n", RegexOptions.Singleline);
    newContent = Regex.Replace(newContent, "(?<=<!-- suppressions -->\\r?\\n).*(?=<!-- suppressions -->)", "\n" + GenerateSuppressorsTable(diagnosticSuppressors) + "\n", RegexOptions.Singleline);
    newContent = Regex.Replace(newContent, "(?<=<!-- refactorings -->\\r?\\n).*(?=<!-- refactorings -->)", "\n" + GenerateRefactoringsTable(codeRefactoringProviders) + "\n", RegexOptions.Singleline);
    WriteFileIfChanged(readmePath, newContent);
}

// Update doc readme
{
    var path = outputFolder / "docs" / "README.md";
    Console.WriteLine(path);
    WriteFileIfChanged(path, sb.ToString());
}

// Update title in rule pages and add links to source code
{
    void ValidateRuleDocumentationContainsConfigurationKeys(FullPath path, string ruleId, string content)
    {
        if (!ruleConfigurationKeys.TryGetValue(ruleId, out var configurationKeys))
            return;

        foreach (var configurationKey in configurationKeys)
        {
            if (content.Contains(configurationKey, StringComparison.Ordinal))
                continue;

            documentationValidationErrorCount++;
            Console.Error.WriteLine($"Missing configuration key '{configurationKey}' in {path.MakePathRelativeTo(outputFolder)}");
        }
    }

    var rules = new HashSet<string>(StringComparer.Ordinal);
    foreach (var diagnosticAnalyzer in diagnosticAnalyzers)
    {
        foreach (var diagnostic in diagnosticAnalyzer.SupportedDiagnostics)
        {
            if (!rules.Add(diagnostic.Id))
                continue;

            var title = $"# {diagnostic.Id} - {EscapeMarkdown(diagnostic.Title.ToString(CultureInfo.InvariantCulture))}";
            var detailPath = outputFolder / "docs" / "Rules" / (diagnostic.Id + ".md");
            if (File.Exists(detailPath))
            {
                var lines = (await File.ReadAllLinesAsync(detailPath)).ToList();
                lines[0] = title;

                if (!lines.Any(line => line.Contains("<!-- sources -->", StringComparison.Ordinal)))
                {
                    lines.Insert(1, "<!-- sources -->");
                    lines.Insert(1, "<!-- sources -->");
                }

                var newContent = string.Join('\n', lines) + "\n";

                var sourceLinks = new List<string>();
                string GetFilePath(string name)
                {
                    try
                    {
                        var files = Directory.GetFiles(outputFolder / "src", name + ".cs", SearchOption.AllDirectories);
                        if (files.Length == 0)
                        {
                            files = Directory.GetFiles(outputFolder / "src", name + "." + diagnostic.Id + ".cs", SearchOption.AllDirectories);
                        }
                        if (files.Length == 0)
                        {
                            files = Directory.GetFiles(outputFolder / "src", name + ".*.cs", SearchOption.AllDirectories);
                        }

                        if (files.Length == 0)
                            throw new InvalidOperationException($"Cannot find source file for {name}");

                        if (files.Length > 1)
                            throw new InvalidOperationException($"Cannot find source file for {name}");

                        var sourceFile = FullPath.FromPath(files.Single());
                        var relativePath = sourceFile.MakePathRelativeTo(outputFolder);
                        return "https://github.com/meziantou/Meziantou.Analyzer/blob/main/" + relativePath.Replace('\\', '/');
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Cannot find source file for {name}", ex);
                    }
                }
                void AddLink(string name)
                {
                    var url = GetFilePath(name);
                    var text = Path.GetFileName(url);
                    sourceLinks.Add($"[{text}]({url})");
                }

                foreach (var analyzer in diagnosticAnalyzers)
                {
                    if (analyzer.SupportedDiagnostics.Any(d => d.Id == diagnostic.Id))
                    {
                        AddLink(analyzer.GetType().Name);
                    }
                }

                var fixers = codeFixProviders.Where(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal)).ToArray();
                foreach (var fixer in fixers)
                {
                    AddLink(fixer.GetType().Name);
                }

                sourceLinks.Sort(StringComparer.Ordinal);
                newContent = Regex.Replace(newContent, "(?<=<!-- sources -->\\r?\\n).*(?=<!-- sources -->)", (sourceLinks.Count == 1 ? "Source: " : "Sources: ") + string.Join(", ", sourceLinks) + "\n", RegexOptions.Singleline);

                ValidateRuleDocumentationContainsConfigurationKeys(detailPath, diagnostic.Id, newContent);
                WriteFileIfChanged(detailPath, newContent);
            }
            else
            {
                WriteFileIfChanged(detailPath, title);
                ValidateRuleDocumentationContainsConfigurationKeys(detailPath, diagnostic.Id, title);
            }
        }
    }
}

// Update editorconfig files for NuGet package
{
    GenerateFile(outputFolder / "src" / "Meziantou.Analyzer.Pack" / "configuration" / "none.editorconfig", sb => GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: "none", appendCodeBlock: false));
    GenerateFile(outputFolder / "src" / "Meziantou.Analyzer.Pack" / "configuration" / "default.editorconfig", sb => GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: null, appendCodeBlock: false));
    GenerateFile(outputFolder / "src" / "Meziantou.Analyzer.Pack" / "configuration" / "all-suggestions.editorconfig", sb => GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: "suggestion", appendCodeBlock: false));
    GenerateFile(outputFolder / "src" / "Meziantou.Analyzer.Pack" / "configuration" / "all-warnings.editorconfig", sb => GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: "warning", appendCodeBlock: false));
    GenerateFile(outputFolder / "src" / "Meziantou.Analyzer.Pack" / "configuration" / "all-errors.editorconfig", sb => GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: "error", appendCodeBlock: false));
    void GenerateFile(FullPath outputPath, Action<StringBuilder> code)
    {
        var content = new StringBuilder();
        content.Append("# This file is generated by the build process. Do not edit it manually.\n");
        content.Append("is_global = true\n");
        content.Append("global_level = -100\n");
        content.Append('\n');
        code(content);
        WriteFileIfChanged(outputPath, content.ToString());
    }
}

if (fileWritten > 0)
{
    Console.WriteLine($"{fileWritten} file(s) updated.");
    Console.WriteLine();
    Console.WriteLine("Changes:");

    var psi = new System.Diagnostics.ProcessStartInfo
    {
        FileName = "git",
        Arguments = "--no-pager diff",
        WorkingDirectory = outputFolder.Value,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };
    var process = System.Diagnostics.Process.Start(psi)!;
    process.OutputDataReceived += (sender, e) => { if (e.Data is not null) Console.WriteLine(e.Data); };
    process.ErrorDataReceived += (sender, e) => { if (e.Data is not null) Console.WriteLine(e.Data); };
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    await process.WaitForExitAsync();
}

if (documentationValidationErrorCount > 0)
{
    Console.Error.WriteLine($"{documentationValidationErrorCount} documentation validation error(s) found.");
    return 1;
}

return fileWritten;

void WriteFileIfChanged(FullPath path, string content)
{
    var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    content = content.ReplaceLineEndings("\n");
    if (!File.Exists(path))
    {
        path.CreateParentDirectory();
        File.WriteAllText(path, content, encoding);
        fileWritten++;
        Console.WriteLine($"Created file: {path}");
        return;
    }

    var existingContent = File.ReadAllText(path).ReplaceLineEndings("\n");
    if (existingContent.TrimEnd() != content.TrimEnd())
    {
        File.WriteAllText(path, content, encoding);
        fileWritten++;
        Console.WriteLine($"Updated file: {path}");
    }
}

static string GenerateRulesTable(List<DiagnosticAnalyzer> diagnosticAnalyzers, List<CodeFixProvider> codeFixProviders, IReadOnlyDictionary<string, IReadOnlyList<string>> ruleConfigurationKeys, bool addTitle = true)
{
    var sb = new StringBuilder();
    sb.Append("|Id|Category|Description|Severity|Is enabled|Code fix|Configurable|\n");
    sb.Append("|--|--------|-----------|:------:|:--------:|:------:|:----------:|\n");

    foreach (var diagnostic in diagnosticAnalyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id, StringComparer.Ordinal))
    {
        if (!diagnostic.HelpLinkUri.Contains(diagnostic.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid help link for " + diagnostic.Id);
        }

        var hasCodeFix = codeFixProviders.Exists(codeFixProvider => codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal));
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

            ruleConfigurationKeys.TryGetValue(diagnostic.Id, out var configurationKeys);
            configurationKeys ??= [];

        sb.Append('|')
          .Append(GetBoolean(diagnostic.IsEnabledByDefault))
          .Append('|')
          .Append(GetBoolean(hasCodeFix))
                    .Append('|');

                if (configurationKeys.Count > 0 && addTitle)
                {
                        sb.Append("<span title='")
                            .Append(HtmlEncoder.Default.Encode(string.Join("\n", configurationKeys)))
                            .Append("'>")
                            .Append(GetBoolean(true))
                            .Append("</span>");
                }
                else
                {
                        sb.Append(GetBoolean(configurationKeys.Count > 0));
                }

                sb.Append('|')
                    .Append('\n');
    }

    return sb.ToString();
}

[SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "The url must be lowercase")]
static string GenerateSuppressorsTable(List<DiagnosticSuppressor> diagnosticSuppressors)
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
        else if (suppression.SuppressedDiagnosticId.StartsWith("IDE", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append('[')
              .Append(suppression.SuppressedDiagnosticId)
              .Append("](")
              .Append($"https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/").Append(suppression.SuppressedDiagnosticId.ToLowerInvariant()).Append("?WT.mc_id=DT-MVP-5003978")
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

static string GenerateRefactoringsTable(List<CodeRefactoringProvider> codeRefactoringProviders)
{
    var sb = new StringBuilder();
    sb.Append("|Name|\n");
    sb.Append("|----|\n");

    foreach (var refactoring in codeRefactoringProviders.OrderBy(r => r.GetType().Name, StringComparer.Ordinal))
    {
        var typeName = refactoring.GetType().Name;
        var displayName = typeName.EndsWith("Refactoring", StringComparison.Ordinal)
            ? typeName.Substring(0, typeName.Length - "Refactoring".Length)
            : typeName;

        sb.Append("|`")
          .Append(displayName)
          .Append("`|\n");
    }

    return sb.ToString();
}

static void GenerateEditorConfig(StringBuilder sb, List<DiagnosticAnalyzer> analyzers, string? overrideSeverity = null, bool appendCodeBlock = true)
{
    if (appendCodeBlock)
    {
        sb.Append("```editorconfig\n");
    }

    var first = true;
    foreach (var diagnostic in analyzers.SelectMany(diagnosticAnalyzer => diagnosticAnalyzer.SupportedDiagnostics).DistinctBy(diag => diag.Id).OrderBy(diag => diag.Id, StringComparer.Ordinal))
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

    if (appendCodeBlock)
    {
        sb.Append("```\n");
    }
}

static string GetSeverity(DiagnosticSeverity severity)
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

static string EscapeMarkdown(string text)
{
    return text
        .Replace("[", "\\[", StringComparison.Ordinal)
        .Replace("]", "\\]", StringComparison.Ordinal)
        .Replace("<", "\\<", StringComparison.Ordinal)
        .Replace(">", "\\>", StringComparison.Ordinal);
}

static string GetBoolean(bool value)
{
    return value ? "✔️" : "❌";
}

static IReadOnlyDictionary<string, IReadOnlyList<string>> GetRuleConfigurationKeys(IEnumerable<Assembly> assemblies)
{
    var configurationDefinitionType = typeof(ConfigurationDefinition<bool>).GetGenericTypeDefinition();
    var keyPropertyName = nameof(ConfigurationDefinition<bool>.Key);
    var isHiddenPropertyName = nameof(ConfigurationDefinition<bool>.IsHidden);
    var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

    foreach (var type in assemblies.SelectMany(assembly => assembly.GetTypes()))
    {
        foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!field.FieldType.IsGenericType || field.FieldType.GetGenericTypeDefinition() != configurationDefinitionType)
                continue;

            var fieldValue = field.GetValue(null);
            if (fieldValue is null)
                continue;

            if (field.FieldType.GetProperty(isHiddenPropertyName)?.GetValue(fieldValue) is bool isHidden && isHidden)
                continue;

            if (field.FieldType.GetProperty(keyPropertyName)?.GetValue(fieldValue) is not string key)
                continue;

            if (TryGetRuleIdPrefix(key, out var ruleId) is false)
                continue;

            if (!result.TryGetValue(ruleId, out var keys))
            {
                keys = [];
                result.Add(ruleId, keys);
            }

            keys.Add(key);
        }
    }

    var output = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
    foreach (var item in result)
    {
        output[item.Key] = [.. item.Value.Order(StringComparer.Ordinal)];
    }

    return output;
}

static bool TryGetRuleIdPrefix(string key, [NotNullWhen(true)] out string? ruleId)
{
    ruleId = null;
    if (key.Length < 6)
        return false;

    if (key[0] != 'M' || key[1] != 'A')
        return false;

    if (!char.IsAsciiDigit(key[2]) || !char.IsAsciiDigit(key[3]) || !char.IsAsciiDigit(key[4]) || !char.IsAsciiDigit(key[5]))
        return false;

    if (key.Length > 6 && key[6] != '.')
        return false;

    ruleId = key.Substring(0, 6);
    return true;
}
