namespace Meziantou.Analyzer.Configurations;

public sealed class ConfigurationDefinition<T>
{
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
    public T DefaultValue { get; }
}