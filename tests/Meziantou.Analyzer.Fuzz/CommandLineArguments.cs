using System.Globalization;

namespace Meziantou.Analyzer.Fuzz;

/// <summary>
/// The arguments of a subcommand: <c>--name value</c> options, <c>--name</c> flags (when followed by another option or nothing), and positional values.
/// </summary>
internal sealed class CommandLineArguments
{
    private static readonly HashSet<string> Flags = new(StringComparer.Ordinal) { "no-fixes", "json" };

    private readonly Dictionary<string, string?> _options = new(StringComparer.Ordinal);

    public List<string> Positional { get; } = [];

    public static CommandLineArguments Parse(ReadOnlySpan<string> args)
    {
        var result = new CommandLineArguments();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var name = arg[2..];
                if (Flags.Contains(name) || i + 1 >= args.Length)
                {
                    result._options[name] = null;
                }
                else
                {
                    result._options[name] = args[i + 1];
                    i++;
                }
            }
            else
            {
                result.Positional.Add(arg);
            }
        }

        return result;
    }

    public bool HasFlag(string name) => _options.ContainsKey(name);

    public string? GetString(string name) => _options.GetValueOrDefault(name);

    public string GetRequiredString(string name) => GetString(name) ?? throw new ArgumentException($"The option '--{name}' is required", nameof(name));

    public int GetInt32(string name, int defaultValue) => GetString(name) is { } value ? int.Parse(value, CultureInfo.InvariantCulture) : defaultValue;
}
