using System.Globalization;
using System.Text.Json;
using Meziantou.Framework;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// A fuzzing session: start one libFuzzer worker per core until the deadline, triage the inputs that stop a worker,
/// then merge the corpus for the next session and minimize the inputs of the findings.
/// </summary>
/// <remarks>
/// The output folder contains:
/// <list type="bullet">
///   <item><c>seeds/</c>: the snippets of the tests, and <c>csharp.dict</c>, the libFuzzer dictionary</item>
///   <item><c>corpus/</c>: the inputs discovered by the workers, shared by all of them</item>
///   <item><c>corpus-merged/</c>: the minimal corpus that covers the same code as the corpus and the previous corpus</item>
///   <item><c>findings/</c>: one folder per finding, see <see cref="FindingStore"/></item>
///   <item><c>artifacts/</c>, <c>logs/</c>, <c>state/</c>: the crashes saved by libFuzzer, the output of each run, and the files of the workers</item>
/// </list>
/// </remarks>
internal sealed class Campaign
{
    private const int MaxInputLength = 16 * 1024;
    private const int MaxConsecutiveUnexplainedStops = 5;

    private readonly FullPath _libFuzzer;
    private readonly FullPath _output;
    private readonly FullPath? _previousCorpus;
    private readonly int _timeoutSeconds;
    private readonly Lock _suppressedComponentsLock = new();

    private Campaign(FullPath libFuzzer, FullPath output, FullPath? previousCorpus, int timeoutSeconds)
    {
        _libFuzzer = libFuzzer;
        _output = output;
        _previousCorpus = previousCorpus is { } path && Directory.Exists(path) && Directory.EnumerateFiles(path).Any() ? path : null;
        _timeoutSeconds = timeoutSeconds;
    }

    private FullPath SeedsDirectory => _output / "seeds";
    private FullPath DictionaryPath => _output / "seeds" / "csharp.dict";
    private FullPath CorpusDirectory => _output / "corpus";
    private FullPath MergedCorpusDirectory => _output / "corpus-merged";
    private FullPath FindingsDirectory => _output / "findings";
    private FullPath ArtifactsDirectory => _output / "artifacts";
    private FullPath LogsDirectory => _output / "logs";
    private FullPath StateDirectory => _output / "state";
    private FullPath SuppressedComponentsFile => _output / "state" / "suppressed.txt";

    public static async Task<int> RunAsync(CommandLineArguments arguments)
    {
        var campaign = new Campaign(
            FullPath.FromPath(arguments.GetRequiredString("libfuzzer")),
            FullPath.FromPath(arguments.GetRequiredString("output")),
            arguments.GetString("previous-corpus") is { } previousCorpus ? FullPath.FromPath(previousCorpus) : null,
            arguments.GetInt32("timeout", 30));

        var workerCount = arguments.GetInt32("workers", Environment.ProcessorCount);
        var duration = TimeSpan.FromMinutes(arguments.GetInt32("duration", 60));
        var mergeDuration = TimeSpan.FromMinutes(arguments.GetInt32("merge-minutes", 30));
        var minimizeDuration = TimeSpan.FromMinutes(arguments.GetInt32("minimize-minutes", 30));

        foreach (var directory in new[] { campaign.CorpusDirectory, campaign.FindingsDirectory, campaign.ArtifactsDirectory, campaign.LogsDirectory, campaign.StateDirectory })
        {
            Directory.CreateDirectory(directory);
        }

        var seedCount = CorpusExtractor.Extract(FullPath.FromPath(arguments.GetRequiredString("tests")), campaign.SeedsDirectory, campaign.DictionaryPath, workerCount);
        Log($"{seedCount} seeds, {workerCount} workers, fuzzing for {duration}");

        var deadline = DateTimeOffset.UtcNow + duration;
        await Task.WhenAll(Enumerable.Range(0, workerCount).Select(index => campaign.RunWorkerAsync(index, deadline))).ConfigureAwait(false);

        await campaign.MergeCorpusAsync(mergeDuration).ConfigureAwait(false);
        await campaign.MinimizeFindingsAsync(minimizeDuration).ConfigureAwait(false);
        campaign.PrintSummary();
        return 0;
    }

    private async Task RunWorkerAsync(int index, DateTimeOffset deadline)
    {
        var currentInputFile = StateDirectory / $"worker{index}.input";
        var consecutiveUnexplainedStops = 0;
        for (var run = 0; ; run++)
        {
            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining < TimeSpan.FromMinutes(1))
                return;

            var name = $"worker{index}-run{run}";
            var logFile = LogsDirectory / $"{name}.log";
            var artifactPrefix = $"{ArtifactsDirectory / name}-";
            List<string> libFuzzerArguments =
            [
                .. GetCommonArguments(),
                $"-seed={(index * 1000) + run + 1}",
                $"-max_total_time={(int)remaining.TotalSeconds}",
                $"-artifact_prefix={artifactPrefix}",
                "-print_final_stats=1",
                .. GetHarnessArguments(),
                CorpusDirectory,
                SeedsDirectory / $"shard-{index}",
            ];

            if (_previousCorpus is { } previousCorpus)
            {
                libFuzzerArguments.Add(previousCorpus);
            }

            var environment = GetHarnessEnvironment(saveFindings: true);
            environment[FuzzEnvironment.CurrentInputFileVariable] = currentInputFile;

            Log($"[{name}] Starting libFuzzer for {remaining}");
            var result = await ChildProcess.RunAsync(_libFuzzer, libFuzzerArguments, environment, logFile, remaining + TimeSpan.FromMinutes(5), isolateProcessGroup: true, CancellationToken.None).ConfigureAwait(false);
            if (result.ExitCode is 0)
                return;

            // libFuzzer saves the inputs that time out or crash, but the harness is another process, so when it dies without reporting
            // the crash to libFuzzer, the input is the last one the harness wrote
            var timeoutArtifact = Directory.EnumerateFiles(ArtifactsDirectory, $"{name}-timeout-*").FirstOrDefault();
            var crashArtifact = Directory.EnumerateFiles(ArtifactsDirectory, $"{name}-crash-*").FirstOrDefault();
            var (kind, input) = (timeoutArtifact, crashArtifact) switch
            {
                ({ } path, _) => ("timeout", FullPath.FromPath(path)),
                (_, { } path) => ("crash", FullPath.FromPath(path)),
                _ => ("crash", CopyCurrentInput(currentInputFile, FullPath.FromPath($"{artifactPrefix}process-crash"))),
            };

            Log($"[{name}] libFuzzer stopped with exit code {result.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "(timeout)"}, triaging the {kind} of {input}");
            var finding = File.Exists(input) ? await Triage.RunAsync(kind, input, logFile, TimeSpan.FromSeconds(_timeoutSeconds * 2), CancellationToken.None).ConfigureAwait(false) : null;
            if (finding is null || finding.Component is "unknown")
            {
                consecutiveUnexplainedStops++;
                Log($"[{name}] Cannot find the component responsible for the {kind} ({consecutiveUnexplainedStops} consecutive)");
                if (consecutiveUnexplainedStops >= MaxConsecutiveUnexplainedStops)
                {
                    Log($"[{name}] Stopping the worker");
                    return;
                }
            }
            else
            {
                consecutiveUnexplainedStops = 0;
            }

            if (finding is not null)
            {
                Log($"[{name}] {finding.Title} ({finding.Signature})");
                FindingStore.Save(FindingsDirectory, finding, Triage.ReadInput(input));
                if (finding.Component is not "unknown")
                {
                    // Do not run the component anymore, so the workers find other issues instead of this one again
                    lock (_suppressedComponentsLock)
                    {
                        File.AppendAllLines(SuppressedComponentsFile, [finding.Component]);
                    }
                }
            }
        }
    }

    private async Task MergeCorpusAsync(TimeSpan duration)
    {
        Directory.CreateDirectory(MergedCorpusDirectory);
        List<string> arguments = ["-merge=1", .. GetCommonArguments(), $"-artifact_prefix={ArtifactsDirectory / "merge"}-", .. GetHarnessArguments(), MergedCorpusDirectory, CorpusDirectory];
        if (_previousCorpus is { } previousCorpus)
        {
            arguments.Add(previousCorpus);
        }

        Log("Merging the corpus");
        var result = await ChildProcess.RunAsync(_libFuzzer, arguments, GetHarnessEnvironment(saveFindings: true), LogsDirectory / "merge.log", duration, isolateProcessGroup: true, CancellationToken.None).ConfigureAwait(false);
        if (result.ExitCode is 0)
            return;

        Log($"Cannot merge the corpus (exit code: {result.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "(timeout)"}), keeping all the inputs");
        foreach (var directory in new[] { CorpusDirectory, _previousCorpus })
        {
            if (directory is null)
                continue;

            foreach (var file in Directory.EnumerateFiles(directory.Value))
            {
                File.Copy(file, MergedCorpusDirectory / Path.GetFileName(file), overwrite: true);
            }
        }
    }

    /// <summary>
    /// Ask libFuzzer to minimize the smallest input of each finding: the harness throws for this finding only, so libFuzzer considers the input as a crash.
    /// The crashes and the timeouts are not minimized, as libFuzzer does not consider the death of the harness process as a crash.
    /// </summary>
    private async Task MinimizeFindingsAsync(TimeSpan duration)
    {
        var deadline = DateTimeOffset.UtcNow + duration;
        var engine = new AnalysisEngine(ComponentFilter.Create(only: [], suppressed: new HashSet<string>(StringComparer.Ordinal), runCodeFixes: true));
        foreach (var findingDirectory in Directory.EnumerateDirectories(FindingsDirectory).Select(FullPath.FromPath).Order())
        {
            var remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining < TimeSpan.FromSeconds(30))
            {
                Log("No time left to minimize the other findings");
                return;
            }

            var kind = ReadFindingKind(findingDirectory);
            if (kind is null or FindingKind.Crash or FindingKind.Timeout or FindingKind.RoslynException or FindingKind.HarnessException || FindingStore.GetSmallestInput(findingDirectory) is not { } input)
                continue;

            var signature = findingDirectory.Name;
            var minimized = findingDirectory / FindingStore.MinimizedInputFileName;
            var seconds = (int)Math.Min(120, remaining.TotalSeconds);
            List<string> arguments = ["-minimize_crash=1", $"-max_total_time={seconds}", $"-exact_artifact_path={minimized}", .. GetCommonArguments(), $"-artifact_prefix={StateDirectory / "minimize"}-", .. GetHarnessArguments(), input];
            var environment = GetHarnessEnvironment(saveFindings: false);
            environment[FuzzEnvironment.TargetSignatureVariable] = signature;

            Log($"Minimizing {signature}");
            await ChildProcess.RunAsync(_libFuzzer, arguments, environment, LogsDirectory / $"minimize-{signature}.log", TimeSpan.FromSeconds(seconds + 120), isolateProcessGroup: true, CancellationToken.None).ConfigureAwait(false);

            // libFuzzer keeps the inputs that crash, so make sure the minimized input reproduces the same finding
            if (File.Exists(minimized))
            {
                var findings = await engine.RunAsync(Triage.ReadInput(minimized)).ConfigureAwait(false);
                if (findings.FirstOrDefault(finding => finding.Signature == signature) is { } finding)
                {
                    FindingStore.SaveMinimized(findingDirectory, finding);
                }
                else
                {
                    Log($"The minimized input of {signature} does not reproduce the finding");
                    File.Delete(minimized);
                }
            }
        }
    }

    private void PrintSummary()
    {
        var findings = Directory.EnumerateDirectories(FindingsDirectory)
            .Select(FullPath.FromPath)
            .Select(directory => (Directory: directory, Kind: ReadFindingKind(directory)))
            .ToList();

        Log($"{findings.Count} findings");
        foreach (var group in findings.GroupBy(finding => finding.Kind).OrderBy(group => group.Key))
        {
            Log($"  {group.Key}: {group.Count()}");
        }
    }

    private static FindingKind? ReadFindingKind(FullPath findingDirectory)
    {
        var path = findingDirectory / FindingStore.FindingFileName;
        if (!File.Exists(path))
            return null;

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return Enum.TryParse<FindingKind>(document.RootElement.GetProperty("kind").GetString(), ignoreCase: false, out var kind) ? kind : null;
    }

    private IEnumerable<string> GetCommonArguments()
    {
        yield return $"-timeout={_timeoutSeconds}";
        yield return $"-max_len={MaxInputLength}";
        yield return $"-dict={DictionaryPath}";
    }

    private static IEnumerable<string> GetHarnessArguments()
    {
        // libfuzzer-dotnet starts the harness with "<target_path> <target_arg>", so the harness must be a single file
        var (fileName, arguments) = ChildProcess.GetHarnessCommand();
        yield return $"--target_path={fileName}";
        if (arguments.Length > 0)
        {
            yield return $"--target_arg={arguments[0]}";
        }
    }

    private Dictionary<string, string?> GetHarnessEnvironment(bool saveFindings)
    {
        return new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [FuzzEnvironment.FindingsDirectoryVariable] = saveFindings ? FindingsDirectory : null,
            [FuzzEnvironment.SuppressedComponentsFileVariable] = SuppressedComponentsFile,

            // Turn the inputs that make an analyzer allocate without limit into OutOfMemoryException,
            // instead of letting the operating system kill the process (4 GB)
            ["DOTNET_GCHeapHardLimit"] = "0x100000000",
        };
    }

    private static FullPath CopyCurrentInput(FullPath currentInputFile, FullPath destination)
    {
        if (File.Exists(currentInputFile))
        {
            File.Copy(currentInputFile, destination, overwrite: true);
        }

        return destination;
    }

    private static void Log(string message) => Console.WriteLine($"[{DateTimeOffset.UtcNow:HH:mm:ss}] {message}");
}
