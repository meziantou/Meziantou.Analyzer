using Meziantou.Framework;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// The environment variables that configure the harness. libFuzzer starts the harness with a single argument,
/// so the settings of the fuzzing session cannot be passed on the command line.
/// </summary>
internal static class FuzzEnvironment
{
    /// <summary>The folder where the findings are saved. Nothing is saved when it is not set.</summary>
    public const string FindingsDirectoryVariable = "FUZZ_FINDINGS_DIR";

    /// <summary>A file listing the full names of the analyzers and code fixers not to run, one per line, such as the ones that already crashed the process.</summary>
    public const string SuppressedComponentsFileVariable = "FUZZ_SUPPRESSED_FILE";

    /// <summary>When set, the harness throws for the finding with this signature, so libFuzzer can minimize its input, and saves nothing.</summary>
    public const string TargetSignatureVariable = "FUZZ_TARGET_SIGNATURE";

    /// <summary>The file where the harness writes each input before running it, as libFuzzer does not save the input that kills the harness process.</summary>
    public const string CurrentInputFileVariable = "FUZZ_CURRENT_INPUT_FILE";

    public static FullPath? FindingsDirectory => GetPath(FindingsDirectoryVariable);

    public static FullPath? SuppressedComponentsFile => GetPath(SuppressedComponentsFileVariable);

    public static string? TargetSignature => GetString(TargetSignatureVariable);

    public static FullPath? CurrentInputFile => GetPath(CurrentInputFileVariable);

    public static IReadOnlySet<string> GetSuppressedComponents()
    {
        if (SuppressedComponentsFile is not { } path || !File.Exists(path))
            return new HashSet<string>(StringComparer.Ordinal);

        return File.ReadAllLines(path).Select(line => line.Trim()).Where(line => line.Length > 0).ToHashSet(StringComparer.Ordinal);
    }

    private static string? GetString(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;

    private static FullPath? GetPath(string name) => GetString(name) is { } value ? FullPath.FromPath(value) : null;
}
