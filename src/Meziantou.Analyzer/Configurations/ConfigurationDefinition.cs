namespace Meziantou.Analyzer.Configurations;

public sealed class ConfigurationDefinition<T>
{
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
}
