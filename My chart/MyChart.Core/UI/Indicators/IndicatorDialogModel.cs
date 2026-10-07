using MyChart.Core.Commands;
using MyChart.Core.Contracts.Indicators;
using MyChart.Core.Models.Indicators;

namespace MyChart.Core.UI.Indicators;

/// <summary>One row in the indicator dialog list.</summary>
public sealed record IndicatorDialogRow(
    string Id,
    string IndicatorName,
    IReadOnlyDictionary<string, double> Parameters,
    string Summary);

/// <summary>Catalog entry available to add.</summary>
public sealed record IndicatorCatalogEntry(
    string Name,
    IReadOnlyList<IndicatorParameter> Inputs);

/// <summary>
/// T6.06 Indicator dialog — list, add, edit parameters, remove.
/// Pure model; UI binds to rows and calls controller methods that emit commands.
/// </summary>
public sealed class IndicatorDialogModel
{
    private readonly IndicatorList _list;
    private readonly List<IndicatorCatalogEntry> _catalog;

    public IndicatorDialogModel(IndicatorList list, IReadOnlyList<IndicatorCatalogEntry> catalog)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _catalog = catalog?.ToList() ?? new List<IndicatorCatalogEntry>();
    }

    public bool IsOpen { get; private set; }

    public IReadOnlyList<IndicatorCatalogEntry> Catalog => _catalog;

    public IReadOnlyList<IndicatorDialogRow> Rows
        => _list.Items.Select(i => new IndicatorDialogRow(
            i.Id,
            i.IndicatorName,
            i.Parameters,
            FormatSummary(i))).ToList();

    public void Open() => IsOpen = true;
    public void Close() => IsOpen = false;

    public static string FormatSummary(IndicatorInstance instance)
    {
        if (instance.Parameters.Count == 0)
            return instance.IndicatorName;
        var parts = instance.Parameters.Select(kv => $"{kv.Key}={kv.Value}");
        return instance.IndicatorName + "(" + string.Join(", ", parts) + ")";
    }
}

/// <summary>T6.06 dialog controller — Add / Edit / Remove via IndicatorList commands.</summary>
public sealed class IndicatorDialogController
{
    private readonly IndicatorList _list;
    private readonly IndicatorDialogModel _model;

    public IndicatorDialogController(IndicatorList list, IndicatorDialogModel model)
    {
        _list = list;
        _model = model;
    }

    public IndicatorDialogModel Model => _model;

    public void Open() => _model.Open();
    public void Close() => _model.Close();

    public AddIndicatorCommand CreateAddCommand(string indicatorName, IReadOnlyDictionary<string, double> parameters)
    {
        var id = Guid.NewGuid().ToString("N");
        int hash = 0;
        foreach (var kv in parameters.OrderBy(k => k.Key, StringComparer.Ordinal))
            hash = HashCode.Combine(hash, kv.Key, kv.Value);
        var instance = new IndicatorInstance(id, indicatorName, hash, new Dictionary<string, double>(parameters));
        return new AddIndicatorCommand(_list, instance);
    }

    public EditIndicatorCommand? CreateEditCommand(string id, IReadOnlyDictionary<string, double> parameters)
    {
        var existing = _list.Find(id);
        if (existing is null) return null;
        int hash = 0;
        foreach (var kv in parameters.OrderBy(k => k.Key, StringComparer.Ordinal))
            hash = HashCode.Combine(hash, kv.Key, kv.Value);
        var next = existing with
        {
            Parameters = new Dictionary<string, double>(parameters),
            ParameterHash = hash
        };
        return new EditIndicatorCommand(_list, existing, next);
    }

    public RemoveIndicatorCommand? CreateRemoveCommand(string id)
    {
        var existing = _list.Find(id);
        if (existing is null) return null;
        return new RemoveIndicatorCommand(_list, existing);
    }

    public void Add(string indicatorName, IReadOnlyDictionary<string, double> parameters)
        => CreateAddCommand(indicatorName, parameters).Execute();

    public bool Edit(string id, IReadOnlyDictionary<string, double> parameters)
    {
        var cmd = CreateEditCommand(id, parameters);
        if (cmd is null) return false;
        cmd.Execute();
        return true;
    }

    public bool Remove(string id)
    {
        var cmd = CreateRemoveCommand(id);
        if (cmd is null) return false;
        cmd.Execute();
        return true;
    }
}
