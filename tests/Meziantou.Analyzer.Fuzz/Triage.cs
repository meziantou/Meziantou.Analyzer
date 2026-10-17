using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Meziantou.Framework;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// Find the analyzer or the code fixer responsible for an input that stops libFuzzer: the harness process crashed, or did not complete in time.
/// </summary>
internal static class Triage
{
    public static async Task<int> RunCommandAsync(CommandLineArguments arguments)
    {
        var kind = arguments.GetRequiredString("kind");
        var input = FullPath.FromPath(arguments.GetRequiredString("input"));
        var log = arguments.GetString("log") is { } logPath ? FullPath.FromPath(logPath) : (FullPath?)null;
        var timeout = TimeSpan.FromSeconds(arguments.GetInt32("timeout", 60));

        var finding = await RunAsync(kind, input, log, timeout, CancellationToken.None).ConfigureAwait(false);
        if (finding is null)
        {
            Console.WriteLine("The input does not reproduce the issue");
            return 0;
        }

        Console.WriteLine(JsonSerializer.Serialize(finding, FindingStore.JsonOptions));
        return 1;
    }

    /// <summary>Find the component responsible for the input, or <see langword="null"/> when the input does not reproduce the issue.</summary>
    public static async Task<Finding?> RunAsync(string kind, FullPath input, FullPath? log, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (kind is "timeout")
            return await FindComponentThatDoesNotCompleteAsync(input, timeout, cancellationToken).ConfigureAwait(false);

        var output = log is { } logPath && File.Exists(logPath) ? await File.ReadAllTextAsync(logPath, cancellationToken).ConfigureAwait(false) : "";
        if (StackTraceParser.GetAnalyzerFrames(output).IsEmpty)
        {
            var result = await ChildProcess.RunHarnessAsync(["repro", input], timeout, cancellationToken).ConfigureAwait(false);
            if (result.TimedOut)
                return await FindComponentThatDoesNotCompleteAsync(input, timeout, cancellationToken).ConfigureAwait(false);

            if (!result.IsHarnessCrash)
                return null;

            output = result.Output;
        }

        var frames = StackTraceParser.GetAnalyzerFrames(output);
        var component = Components.FindByStackFrames(frames);
        return new Finding
        {
            Kind = FindingKind.Crash,
            Component = component is null ? "unknown" : Components.GetName(component),
            DiagnosticIds = component is null ? [] : Components.GetDiagnosticIds(component),
            Message = GetCrashReason(output),
            StackTrace = StackTraceParser.GetFrameLines(output, maxCount: 100),
            Frames = frames,
        };
    }

    private static string? GetCrashReason(string output)
    {
        if (output.Contains("Stack overflow", StringComparison.Ordinal))
            return "stack overflow";

        if (output.Contains("OutOfMemoryException", StringComparison.Ordinal))
            return "out of memory";

        return output.Split('\n').FirstOrDefault(line => line.StartsWith("Unhandled exception.", StringComparison.Ordinal))?.Trim();
    }

    /// <summary>
    /// Replay the input with groups of components, keeping the group that does not complete, until one component remains.
    /// The analyzers run without the code fixers first, as the code fixers need the diagnostics of all the analyzers.
    /// </summary>
    private static async Task<Finding?> FindComponentThatDoesNotCompleteAsync(FullPath input, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var analyzersResult = await ChildProcess.RunHarnessAsync(["repro", "--no-fixes", input], timeout, cancellationToken).ConfigureAwait(false);
        ImmutableArray<object> candidates;
        string[] extraArguments;
        if (analyzersResult.TimedOut)
        {
            candidates = [.. Components.Analyzers];
            extraArguments = ["--no-fixes"];
        }
        else
        {
            var allResult = await ChildProcess.RunHarnessAsync(["repro", input], timeout, cancellationToken).ConfigureAwait(false);
            if (!allResult.TimedOut)
                return null;

            candidates = [.. Components.CodeFixProviders];
            extraArguments = [];
        }

        var groupCount = Math.Max(2, Environment.ProcessorCount);
        while (candidates.Length > 1)
        {
            var groups = candidates.Chunk((candidates.Length + groupCount - 1) / groupCount).ToList();
            var results = await Task.WhenAll(groups.Select(group =>
                ChildProcess.RunHarnessAsync(["repro", "--only", string.Join(',', group.Select(Components.GetName)), .. extraArguments, input], timeout, cancellationToken))).ConfigureAwait(false);

            var index = Array.FindIndex(results, result => result.TimedOut);
            if (index < 0)
            {
                // The components only hang together, or the hang is not deterministic
                candidates = [];
                break;
            }

            candidates = [.. groups[index]];
        }

        var component = candidates.Length is 1 ? candidates[0] : null;
        return new Finding
        {
            Kind = FindingKind.Timeout,
            Component = component is null ? "unknown" : Components.GetName(component),
            DiagnosticIds = component is null ? [] : Components.GetDiagnosticIds(component),
            Message = $"The input is not analyzed within {timeout.TotalSeconds:F0} seconds",
        };
    }

    public static string ReadInput(FullPath path) => Encoding.UTF8.GetString(File.ReadAllBytes(path));
}
