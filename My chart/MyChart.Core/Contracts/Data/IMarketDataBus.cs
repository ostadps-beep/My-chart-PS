namespace MyChart.Core.Contracts.Data;

/// <summary>
/// T1.07 — IMarketDataBus { IDisposable Subscribe&lt;T&gt;(Action&lt;T&gt;); void Publish&lt;T&gt;(T evt) }
/// </summary>
public interface IMarketDataBus
{
    IDisposable Subscribe<T>(Action<T> handler);
    void Publish<T>(T evt);
}
