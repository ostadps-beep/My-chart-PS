namespace MyChart.PluginHost.Registries;

public sealed class DuplicateRegistrationException : Exception
{
    public string Key { get; }
    public DuplicateRegistrationException(string key)
        : base($"Duplicate registration for key '{key}'.")
    {
        Key = key;
    }
}
