using System.Text.RegularExpressions;

namespace Meziantou.Analyzer.Configurations;

public sealed class ConfigurationDefinition<T>
{
    private RegexOptions? _regexOptions;

    public ConfigurationDefinition(string key)
    {
        Key = key;
        HasDefaultValue = false;
    }

    public ConfigurationDefinition(string key, T defaultValue)
    {
        Key = key;
        DefaultValue = defaultValue;
        HasDefaultValue = true;
    }

    public string Key { get; }
    public bool HasDefaultValue { get; }
    public T DefaultValue { get; } = default!;
    public bool IsHidden { get; set; }

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
