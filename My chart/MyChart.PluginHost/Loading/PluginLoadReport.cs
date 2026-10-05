namespace MyChart.PluginHost.Loading;

public sealed class PluginLoadReport
{
    public List<string> Loaded { get; } = new();
    public List<FailedComponent> Failed { get; } = new();

    public sealed record FailedComponent(string ComponentId, string Code, string Message);

    public void AddFailure(string componentId, string code, string message)
        => Failed.Add(new FailedComponent(componentId, code, message));
}
