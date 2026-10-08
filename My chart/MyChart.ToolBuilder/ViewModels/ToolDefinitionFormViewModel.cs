namespace MyChart.ToolBuilder.ViewModels;

/// <summary>
/// PG6.01 thin WPF-facing view model over Core ToolDefinitionFormModel.
/// Keeps UI free of JSON typing; Save blocked when invalid.
/// </summary>
public sealed class ToolDefinitionFormViewModel
{
    private readonly MyChart.Core.UI.ToolBuilder.ToolDefinitionFormModel _model;

    public ToolDefinitionFormViewModel(MyChart.Core.UI.ToolBuilder.ToolDefinitionFormModel? model = null)
    {
        _model = model ?? new MyChart.Core.UI.ToolBuilder.ToolDefinitionFormModel();
    }

    public MyChart.Core.UI.ToolBuilder.ToolDefinitionFormModel Model => _model;

    public string Name
    {
        get => _model.Name;
        set => _model.Name = value;
    }

    public string? LastError => _model.ValidationError;

    public bool SaveCommand()
    {
        return _model.TryGetSaveableDefinition() is not null;
    }

    public string? PreviewDefinitionJson()
    {
        return _model.TryGetSaveableDefinition();
    }
}
