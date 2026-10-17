using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Meziantou.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// Create the seed corpus from the code snippets of the tests, which are raw string literals, and the libFuzzer dictionary from the C# tokens.
/// </summary>
internal static partial class CorpusExtractor
{
    private const int MinSnippetLength = 20;

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    // Common identifiers of the APIs the rules know about, so the mutations can produce code that the rules analyze
    private static readonly string[] DictionaryIdentifiers =
    [
        "Task", "ValueTask", "CancellationToken", "ConfigureAwait", "GetAwaiter", "Result", "Wait", "Thread", "Sleep",
        "IDisposable", "IAsyncDisposable", "Dispose", "DisposeAsync", "Equals", "GetHashCode", "ToString", "CompareTo",
        "StringComparison", "StringComparer", "Ordinal", "OrdinalIgnoreCase", "CultureInfo", "InvariantCulture", "IFormatProvider",
        "Enumerable", "Select", "Where", "First", "FirstOrDefault", "Any", "Count", "ToList", "ToArray", "OrderBy",
        "List<int>", "Dictionary<string, int>", "IEnumerable<T>", "Span<byte>", "ReadOnlySpan<char>", "Array", "Empty",
        "Regex", "RegexOptions", "DateTime", "Now", "UtcNow", "DateTimeOffset", "TimeSpan", "Guid", "Encoding",
        "Console", "WriteLine", "Debug", "Assert", "Exception", "ArgumentException", "ArgumentNullException", "nameof",
        "Attribute", "Obsolete", "Flags", "Serializable", "StructLayout", "DllImport", "LibraryImport", "CallerMemberName",
        "EventHandler", "EventArgs", "Lazy", "Lock", "Monitor", "Interlocked", "Volatile", "Stream", "File", "Path",
        "HttpClient", "JsonSerializer", "ILogger", "LoggerMessage", "Fact", "Test", "TestMethod", "Theory", "InlineData",
    ];

    public static int RunCommand(CommandLineArguments arguments)
    {
        var count = Extract(
            FullPath.FromPath(arguments.GetRequiredString("tests")),
            FullPath.FromPath(arguments.GetRequiredString("output")),
            FullPath.FromPath(arguments.GetRequiredString("dictionary")),
            arguments.GetInt32("shards", 1));

        Console.WriteLine($"{count} snippets extracted");
        return 0;
    }

    /// <summary>
    /// Write the snippets to <c>&lt;output&gt;/shard-&lt;n&gt;</c>, so each libFuzzer worker starts with a part of the seeds only,
    /// as running all the seeds before fuzzing takes a long time.
    /// </summary>
    public static int Extract(FullPath testsDirectory, FullPath outputDirectory, FullPath dictionaryPath, int shardCount)
    {
        var snippets = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(testsDirectory, "*.cs", SearchOption.AllDirectories))
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), Components.ParseOptions);
            foreach (var token in tree.GetRoot().DescendantTokens())
            {
                if (!token.IsKind(SyntaxKind.SingleLineRawStringLiteralToken) && !token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
                    continue;

                var snippet = RemoveMarkup(token.ValueText);
                if (snippet.Length < MinSnippetLength || !snippet.AsSpan().ContainsAny('{', ';'))
                    continue;

                snippets.TryAdd(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(snippet))), snippet);
            }
        }

        var index = 0;
        foreach (var (hash, snippet) in snippets)
        {
            var path = outputDirectory / $"shard-{(index % shardCount).ToString(CultureInfo.InvariantCulture)}" / hash;
            path.CreateParentDirectory();
            File.WriteAllText(path, snippet, Utf8NoBom);
            index++;
        }

        dictionaryPath.CreateParentDirectory();
        File.WriteAllLines(dictionaryPath, GetDictionaryEntries().Distinct(StringComparer.Ordinal).Select(FormatDictionaryEntry));
        return snippets.Count;
    }

    // The tests mark the expected diagnostics with [|code|] and {|id:code|}
    private static string RemoveMarkup(string snippet)
    {
        snippet = DiagnosticWithIdMarkupRegex().Replace(snippet, "");
        return snippet.Replace("|}", "", StringComparison.Ordinal).Replace("[|", "", StringComparison.Ordinal).Replace("|]", "", StringComparison.Ordinal);
    }

    private static IEnumerable<string> GetDictionaryEntries()
    {
        foreach (var kind in SyntaxFacts.GetKeywordKinds().Concat(SyntaxFacts.GetContextualKeywordKinds()).Concat(SyntaxFacts.GetPunctuationKinds()))
        {
            var text = SyntaxFacts.GetText(kind);
            if (text.Length > 0)
                yield return text;
        }

        foreach (var text in new[] { "\"\"\"", "$\"", "@\"", "u8", "#nullable enable", "#pragma warning disable", "#if DEBUG", "#endif", "#region", "#endregion", "///", "/// <summary>", "<param name=\"", "<inheritdoc />" })
        {
            yield return text;
        }

        foreach (var identifier in DictionaryIdentifiers)
        {
            yield return identifier;
        }
    }

    // https://llvm.org/docs/LibFuzzer.html#dictionaries
    private static string FormatDictionaryEntry(string value)
    {
        var sb = new StringBuilder("\"");
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            if (b is (byte)'"' or (byte)'\\')
            {
                sb.Append('\\').Append((char)b);
            }
            else if (b is >= 0x20 and < 0x7F)
            {
                sb.Append((char)b);
            }
            else
            {
                sb.Append(CultureInfo.InvariantCulture, $"\\x{b:X2}");
            }
        }

        return sb.Append('"').ToString();
    }

    [GeneratedRegex(@"\{\|[^:|{}]*:", RegexOptions.None, matchTimeoutMilliseconds: 10_000)]
    private static partial Regex DiagnosticWithIdMarkupRegex();
}
