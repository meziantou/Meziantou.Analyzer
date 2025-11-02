#pragma warning disable RS1035
#pragma warning disable CA1849
#pragma warning disable MA0004
#pragma warning disable MA0009
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Meziantou.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

if (!FullPath.CurrentDirectory().TryFindFirstAncestorOrSelf(p => Directory.Exists(p / ".git"), out var outputFolder))
{
    Console.WriteLine("Cannot find the current git folder");
    return 1;
}

var fileWritten = 0;

var assemblies = new[] { typeof(Meziantou.Analyzer.Rules.CommaAnalyzer).Assembly, typeof(Meziantou.Analyzer.Rules.CommaFixer).Assembly };
var diagnosticAnalyzers = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
    .Where(type => !type.IsAbstract && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
    .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
    .ToList();

var codeFixProviders = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
    .Where(type => !type.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(type))
    .Select(type => (CodeFixProvider)Activator.CreateInstance(type)!)
    .ToList();

var diagnosticSuppressors = assemblies.SelectMany(assembly => assembly.GetExportedTypes())
  .Where(type => !type.IsAbstract && typeof(DiagnosticSuppressor).IsAssignableFrom(type))
  .Select(type => (DiagnosticSuppressor)Activator.CreateInstance(type)!)
  .ToList();

var sb = new StringBuilder();
sb.Append("# ").Append(assemblies[0].GetName().Name).Append("'s rules\n");
var rulesTable = GenerateRulesTable(diagnosticAnalyzers, codeFixProviders);
sb.Append(rulesTable);

var suppressorsTable = GenerateSuppressorsTable(diagnosticSuppressors);
sb.Append('\n');
sb.Append(suppressorsTable);

sb.Append("\n\n# .editorconfig - default values\n\n");
GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: null);

sb.Append("\n# .editorconfig - all rules disabled\n\n");
GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: "none");

Console.WriteLine(sb.ToString());

// Update home readme
{
    // The main readme is embedded into the NuGet package and rendered by nuget.org.
    // nuget.org's markdown support is limited. Raw html in table is not supported.
    var readmePath = outputFolder / "README.md";
    var readmeContent = await File.ReadAllTextAsync(readmePath);
    var newContent = Regex.Replace(readmeContent, "(?<=<!-- rules -->\\r?\\n).*(?=<!-- rules -->)", "\n" + GenerateRulesTable(diagnosticAnalyzers, codeFixProviders, addTitle: false) + "\n", RegexOptions.Singleline);
    newContent = Regex.Replace(newContent, "(?<=<!-- suppressions -->\\r?\\n).*(?=<!-- suppressions -->)", "\n" + GenerateSuppressorsTable(diagnosticSuppressors) + "\n", RegexOptions.Singleline);
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
    foreach (var diagnosticAnalyzer in diagnosticAnalyzers)
    {
        foreach (var diagnostic in diagnosticAnalyzer.SupportedDiagnostics)
        {
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

                AddLink(diagnosticAnalyzer.GetType().Name);

                var fixers = codeFixProviders.Where(fixer => fixer.FixableDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal)).ToArray();
                foreach (var fixer in fixers)
                {
                    AddLink(fixer.GetType().Name);
                }

                newContent = Regex.Replace(newContent, "(?<=<!-- sources -->\\r?\\n).*(?=<!-- sources -->)", (sourceLinks.Count == 1 ? "Source: " : "Sources: ") + string.Join(", ", sourceLinks) + "\n", RegexOptions.Singleline);

                WriteFileIfChanged(detailPath, newContent);
            }
            else
            {
                WriteFileIfChanged(detailPath, title);
            }
        }
    }
}

// Update editorconfig files for NuGet package
{
    GenerateFile(outputFolder / "src" / "Meziantou.Analyzer.Pack" / "configuration" / "none.editorconfig", sb => GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: "none", appendCodeBlock: false));
    GenerateFile(outputFolder / "src" / "Meziantou.Analyzer.Pack" / "configuration" / "default.editorconfig", sb => GenerateEditorConfig(sb, diagnosticAnalyzers, overrideSeverity: null, appendCodeBlock: false));
    void GenerateFile(FullPath outputPath, Action<StringBuilder> code)
    {
        var sb = new StringBuilder();
        sb.Append("# This file is generated by the build process. Do not edit it manually.\n");
        sb.Append("is_global = true\n");
        sb.Append("global_level = 0\n");
        sb.Append('\n');
        code(sb);
        WriteFileIfChanged(outputPath, sb.ToString());
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

    var existingContent = File.ReadAllText(path).ReplaceLineEndings();
    if (existingContent != content)
    {
        File.WriteAllText(path, content, encoding);
        fileWritten++;
        Console.WriteLine($"Updated file: {path}");
    }
}

static string GenerateRulesTable(List<DiagnosticAnalyzer> diagnosticAnalyzers, List<CodeFixProvider> codeFixProviders, bool addTitle = true)
{
    var sb = new StringBuilder();
    sb.Append("|Id|Category|Description|Severity|Is enabled|Code fix|\n");
    sb.Append("|--|--------|-----------|:------:|:--------:|:------:|\n");

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

        sb.Append('|')
          .Append(GetBoolean(diagnostic.IsEnabledByDefault))
          .Append('|')
          .Append(GetBoolean(hasCodeFix))
          .Append('|')
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
