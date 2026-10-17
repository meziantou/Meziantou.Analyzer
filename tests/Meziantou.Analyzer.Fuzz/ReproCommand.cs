using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Meziantou.Framework;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// Run the analyzers and the code fixers on files without libFuzzer, to reproduce a finding on any OS.
/// </summary>
internal static class ReproCommand
{
    public static async Task<int> RunAsync(CommandLineArguments arguments)
    {
        var only = arguments.GetString("only")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
        var filter = ComponentFilter.Create(only, FuzzEnvironment.GetSuppressedComponents(), runCodeFixes: !arguments.HasFlag("no-fixes"));
        var json = arguments.HasFlag("json");
        var findingsDirectory = FuzzEnvironment.FindingsDirectory;

        var files = arguments.Positional.SelectMany(GetFiles).ToList();
        if (files.Count is 0)
        {
            Console.Error.WriteLine("No input file");
            return 2;
        }

        var engine = new AnalysisEngine(filter);
        var signatures = new HashSet<string>(StringComparer.Ordinal);
        var findingCount = 0;
        var stopwatch = Stopwatch.StartNew();
        foreach (var file in files)
        {
            var source = Encoding.UTF8.GetString(await File.ReadAllBytesAsync(file).ConfigureAwait(false));
            var findings = await engine.RunAsync(source).ConfigureAwait(false);
            foreach (var finding in findings)
            {
                findingCount++;
                signatures.Add(finding.Signature);
                if (findingsDirectory is { } directory)
                {
                    FindingStore.Save(directory, finding, source);
                }

                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { file = file.Value, finding }, FindingStore.CompactJsonOptions));
                }
                else
                {
                    Console.WriteLine($"{file}: {finding.Title} ({finding.Signature})");
                    if (files.Count is 1)
                    {
                        PrintDetails(finding);
                    }
                }
            }
        }

        Console.Error.WriteLine($"{files.Count} inputs, {findingCount} findings, {signatures.Count} distinct findings, {stopwatch.Elapsed.TotalMilliseconds / files.Count:F0} ms per input");
        return findingCount > 0 ? 1 : 0;
    }

    private static void PrintDetails(Finding finding)
    {
        foreach (var value in new[] { finding.CodeActionTitle, finding.Message, finding.StackTrace, finding.FixedSource })
        {
            if (value is not null)
            {
                Console.WriteLine(value);
                Console.WriteLine();
            }
        }
    }

    private static IEnumerable<FullPath> GetFiles(string path)
    {
        var fullPath = FullPath.FromPath(path);
        if (Directory.Exists(fullPath))
        {
            // libFuzzer names the inputs of the corpus after their hash, without extension
            return Directory.EnumerateFiles(fullPath, "*", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .Select(FullPath.FromPath);
        }

        return [fullPath];
    }
}
