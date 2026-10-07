using MyChart.Core.Commands;
using MyChart.Core.Contracts.Indicators;
using MyChart.Core.Models.Indicators;

namespace MyChart.Core.UI.Indicators;

/// <summary>One row in the indicator dialog list.</summary>
public sealed record IndicatorDialogRow(
    string Id,
    string IndicatorName,
    IReadOnlyDictionary<string, double> Parameters);

/// <summary>
/// T6.06 Indicator dialog model: list, add, edit parameters, remove.
/// Uses IndicatorList + command names (actual bus execute is the shell's job).
/// </summary>
public sealed class IndicatorDialogModel
{
    private readonly IndicatorList _list;
    private readonly List<string> _commands = new();

    public IndicatorDialogModel(IndicatorList list)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public IReadOnlyList<IndicatorDialogRow> Rows =>
        _list.Items.Select(i => new IndicatorDialogRow(i.Id, i.IndicatorName, i.Parameters)).ToList();

    public IReadOnlyList<string> IssuedCommands => _commands;

    public IndicatorInstance Add(string id, string indicatorName, IReadOnlyDictionary<string, double> parameters)
    {
        var instance = new IndicatorInstance(id, indicatorName, ParameterHash(parameters), parameters);
        _list.Add(instance);
        _commands.Add($"AddIndicator:{indicatorName}");
        return instance;
    }

    /// <summary>Create instance from IIndicator defaults, then allow overrides.</summary>
    public IndicatorInstance AddFromDefinition(string id, IIndicator indicator, IReadOnlyDictionary<string, double>? overrides = null)
    {
        var map = indicator.Inputs.ToDictionary(p => p.Key, p => p.DefaultValue);
        if (overrides is not null)
        {
            foreach (var kv in overrides)
                map[kv.Key] = kv.Value;
        }
        return Add(id, indicator.Name, map);
    }

    public bool EditParameters(string id, IReadOnlyDictionary<string, double> parameters)
    {
        var existing = _list.Find(id);
        if (existing is null) return false;
        var next = existing with { Parameters = parameters, ParameterHash = ParameterHash(parameters) };
        _list.Replace(id, next);
        _commands.Add($"EditIndicator:{id}");
        return true;
    }

    public bool Remove(string id)
    {
        if (!_list.RemoveById(id)) return false;
        _commands.Add($"RemoveIndicator:{id}");
        return true;
    }

    public static int ParameterHash(IReadOnlyDictionary<string, double> parameters)
    {
        var h = 17;
        foreach (var kv in parameters.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            h = unchecked(h * 31 + kv.Key.GetHashCode(StringComparison.Ordinal));
            h = unchecked(h * 31 + kv.Value.GetHashCode());
        }
        return h;
    }
}
