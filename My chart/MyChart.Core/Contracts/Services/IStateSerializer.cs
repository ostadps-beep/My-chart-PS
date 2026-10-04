namespace MyChart.Core.Contracts.Services;

public interface IStateSerializer
{
    string Serialize<T>(T value);
    T? Deserialize<T>(string json);
}
