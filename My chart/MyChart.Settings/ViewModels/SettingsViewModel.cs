using MyChart.Settings.Model;
using MyChart.Settings.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyChart.Settings.ViewModels;

public sealed class GroupViewModel
{
    public GroupViewModel(string name, IEnumerable<FieldViewModel> fields)
    {
        Name = name;
        Fields = new ObservableCollection<FieldViewModel>(fields);
    }

    public string Name { get; }
    public ObservableCollection<FieldViewModel> Fields { get; }
}

public sealed class CategoryViewModel : ViewModelBase
{
    public CategoryViewModel(CategoryDefinition definition, IEnumerable<GroupViewModel> groups)
    {
        Definition = definition;
        Groups = new ObservableCollection<GroupViewModel>(groups);
    }

    public CategoryDefinition Definition { get; }
    public string Id => Definition.Id;
    public string Name => Definition.Name;
    public string Title => Definition.Title;
    public string Description => Definition.Description;
    public ObservableCollection<GroupViewModel> Groups { get; }

    public IEnumerable<FieldViewModel> AllFields => Groups.SelectMany(g => g.Fields);
}

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly SettingsController _controller;
    private CategoryViewModel? _selectedCategory;
    private ISettingsUi? _ui;

    public SettingsViewModel(SettingsController controller)
    {
        _controller = controller;
        Categories = BuildCategories();
        SelectedCategory = Categories[0];
        ApplyCommand = new RelayCommand(Apply, () => IsDirty);
        OkCommand = new RelayCommand(Ok);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset, () => SelectedCategory is not null);
        _controller.Changed += OnChanged;
    }

    public ObservableCollection<CategoryViewModel> Categories { get; }
    public ICommand ApplyCommand { get; }
    public ICommand OkCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

    public CategoryViewModel? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (!Set(ref _selectedCategory, value))
                return;
            Raise(nameof(HeaderTitle));
            Raise(nameof(HeaderDescription));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string HeaderTitle => SelectedCategory?.Title ?? "Settings";
    public string HeaderDescription => SelectedCategory?.Description ?? "";
    public bool IsDirty => _controller.IsDirty;
    public string StatusText => IsDirty ? "Unsaved changes" : "Saved";

    public void AttachUi(ISettingsUi ui) => _ui = ui;

    public void RunCommand(string key)
    {
        if (_ui is null)
            return;

        switch (key)
        {
            case "workspace.save.layout":
                _controller.SaveLayout();
                _ui.ShowMessage("Workspace", "Layout saved.");
                break;
            case "workspace.load.layout":
            {
                var path = _ui.PickOpenJson("Load Layout");
                if (path is not null && !_controller.LoadFromPath(path))
                    _ui.ShowMessage("Workspace", "Could not load the selected file.");
                break;
            }
            case "workspace.reset.layout":
                if (_ui.Confirm("Reset Layout", "Restore every setting to its schema default?"))
                    _controller.ResetLayout();
                break;
            case "workspace.export.settings":
            {
                var path = _ui.PickSaveJson("Export Settings", "chart.json");
                if (path is not null)
                    _controller.ExportTo(path);
                break;
            }
            case "workspace.import.settings":
            {
                var path = _ui.PickOpenJson("Import Settings");
                if (path is not null && !_controller.ImportFrom(path))
                    _ui.ShowMessage("Workspace", "Could not import the selected file.");
                break;
            }
            case "advanced.backup.restore":
                if (_ui.Confirm("Backup / Restore", "Write a backup of the current settings now?"))
                {
                    _controller.Backup();
                    _ui.ShowMessage("Backup", "Backup written to settings/chart.backup.json.");
                }
                else if (_ui.Confirm("Backup / Restore", "Restore settings from the backup file?"))
                {
                    if (!_controller.RestoreBackup())
                        _ui.ShowMessage("Backup", "No backup file was found.");
                }
                break;
        }
    }

    private void Apply() => _controller.Apply();

    private void Ok()
    {
        if (_ui is null)
            return;
        _controller.Ok(_ui);
    }

    private void Cancel()
    {
        if (_ui is null)
            return;
        _controller.Cancel(_ui);
    }

    private void Reset()
    {
        if (SelectedCategory is null)
            return;
        _controller.ResetCategory(SelectedCategory.Id);
    }

    private void OnChanged(IReadOnlyList<string> _)
    {
        foreach (var field in Categories.SelectMany(c => c.AllFields))
            field.Refresh();
        Raise(nameof(IsDirty));
        Raise(nameof(StatusText));
        CommandManager.InvalidateRequerySuggested();
    }

    private ObservableCollection<CategoryViewModel> BuildCategories()
    {
        var list = new ObservableCollection<CategoryViewModel>();
        foreach (var category in SettingsSchema.Categories)
        {
            var groups = SettingsSchema.FieldsInCategory(category.Id)
                .GroupBy(f => f.Group)
                .Select(g => new GroupViewModel(
                    g.Key,
                    g.Select(f => new FieldViewModel(f, _controller, this))));
            list.Add(new CategoryViewModel(category, groups));
        }

        return list;
    }
}

