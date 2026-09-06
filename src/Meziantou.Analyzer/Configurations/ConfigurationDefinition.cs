using System.Text.RegularExpressions;

namespace Meziantou.Analyzer.Configurations;

public sealed class ConfigurationDefinition<T>
{
    private RegexOptions? _regexOptions;

    public ConfigurationDefinition(string key)
        : this([key])
    {
    }

    public ConfigurationDefinition(string key, T defaultValue)
        : this([key], defaultValue)
    {
    }

    public ConfigurationDefinition(string[] keys)
    {
        Keys = ImmutableArray.Create(keys);
        HasDefaultValue = false;
    }

    public ConfigurationDefinition(string[] keys, T defaultValue)
    {
        Keys = ImmutableArray.Create(keys);
        DefaultValue = defaultValue;
        HasDefaultValue = true;
    }

    /// <summary>
    /// The current name of the option, followed by its legacy names. The first key set in the configuration wins,
    /// so the legacy names keep working without being documented.
    /// </summary>
    public ImmutableArray<string> Keys { get; }

    /// <summary>
    /// The current name of the option.
    /// </summary>
    public string Key => Keys[0];

    public bool HasDefaultValue { get; }
    public T DefaultValue { get; } = default!;

    /// <summary>
    /// Indicates the value of the option is a regular expression. It is set by assigning <see cref="RegexOptions"/>,
    /// even when the assigned value is <see cref="System.Text.RegularExpressions.RegexOptions.None"/>. MA0220 reports
    /// the configured values of these options when they are not valid regular expressions.
    /// </summary>
    public bool IsRegex => _regexOptions is not null;

    /// <summary>
    /// The options used to create the <see cref="Regex"/> from the value of the option. Setting it marks the option
    /// as a regular expression (<see cref="IsRegex"/>), so it must be set by the rules that use the value as a pattern.
    /// </summary>
    public RegexOptions RegexOptions
    {
        get => _regexOptions.GetValueOrDefault();
        set => _regexOptions = value;
    }
}
