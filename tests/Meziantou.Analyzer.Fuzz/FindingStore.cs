using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Meziantou.Framework;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// Save the inputs that reproduce a finding as <c>&lt;signature&gt;/input-&lt;hash&gt;.cs</c>, each one with the details of the finding
/// it produces in <c>&lt;signature&gt;/input-&lt;hash&gt;.json</c> (the stack trace and the fixed code depend on the input).
/// <c>&lt;signature&gt;/finding.json</c> is the finding of the first input.
/// Several workers save to the same folder, so the files are only created, never updated.
/// </summary>
internal static class FindingStore
{
    public const string FindingFileName = "finding.json";
    public const string MinimizedInputFileName = "minimized.cs";

    // The smallest inputs are kept, as they are the easiest to understand and to minimize
    private const int MaxInputsPerFinding = 5;

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Dictionary<string, int> SmallestInputLengths = new(StringComparer.Ordinal);
    private static readonly Lock SmallestInputLengthsLock = new();

    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        WriteIndented = true,
    };

    public static JsonSerializerOptions CompactJsonOptions { get; } = new(JsonOptions) { WriteIndented = false };

    public static void Save(FullPath directory, Finding finding, string input)
    {
        lock (SmallestInputLengthsLock)
        {
            if (SmallestInputLengths.TryGetValue(finding.Signature, out var length) && length <= input.Length)
                return;

            SmallestInputLengths[finding.Signature] = input.Length;
        }

        var findingDirectory = directory / finding.Signature;
        Directory.CreateDirectory(findingDirectory);
        var json = JsonSerializer.Serialize(finding, JsonOptions);
        var inputPath = findingDirectory / $"input-{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)))}.cs";
        TryCreateFile(findingDirectory / FindingFileName, json);
        TryCreateFile(GetFindingPath(inputPath), json);
        TryCreateFile(inputPath, input);

        foreach (var file in GetInputs(findingDirectory).Skip(MaxInputsPerFinding))
        {
            try
            {
                File.Delete(file);
                File.Delete(GetFindingPath(file));
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>Save the finding produced by the minimized input of a finding.</summary>
    public static void SaveMinimized(FullPath findingDirectory, Finding finding)
    {
        File.WriteAllText(GetFindingPath(findingDirectory / MinimizedInputFileName), JsonSerializer.Serialize(finding, JsonOptions), Utf8NoBom);
    }

    /// <summary>The file that contains the finding produced by an input.</summary>
    public static FullPath GetFindingPath(FullPath inputPath) => inputPath.ChangeExtension(".json");

    /// <summary>The inputs of a finding, the smallest first.</summary>
    public static IEnumerable<FullPath> GetInputs(FullPath findingDirectory)
    {
        return Directory.EnumerateFiles(findingDirectory, "input-*.cs")
            .Select(path => new FileInfo(path))
            .OrderBy(file => file.Length)
            .ThenBy(file => file.Name, StringComparer.Ordinal)
            .Select(file => FullPath.FromPath(file.FullName));
    }

    public static FullPath? GetSmallestInput(FullPath findingDirectory) => GetInputs(findingDirectory).Select(path => (FullPath?)path).FirstOrDefault();

    private static void TryCreateFile(FullPath path, string content)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, Utf8NoBom);
            writer.Write(content);
        }
        catch (IOException) when (File.Exists(path))
        {
            // Another worker saved the same file
        }
    }
}
