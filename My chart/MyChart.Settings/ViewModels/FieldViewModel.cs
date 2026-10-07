using MyChart.Settings.Model;
using MyChart.Settings.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MyChart.Settings.ViewModels;

public sealed class CheckOptionViewModel : ViewModelBase
{
    private readonly FieldViewModel _owner;
    private bool _isChecked;

    public CheckOptionViewModel(FieldViewModel owner, string name, bool isChecked)
    {
        _owner = owner;
        Name = name;
        _isChecked = isChecked;
    }

    public string Name { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (!Set(ref _isChecked, value))
                return;
            _owner.CommitCheckList();
        }
    }

    public void SetCheckedSilent(bool value)
    {
        _isChecked = value;
        Raise(nameof(IsChecked));
    }
}

public sealed class ParameterRowViewModel : ViewModelBase
{
    private readonly FieldViewModel _owner;
    private string _name;
    private string _value;

    public ParameterRowViewModel(FieldViewModel owner, ParameterEntry entry)
    {
        _owner = owner;
        _name = entry.Name;
        _value = entry.Value;
        RemoveCommand = new RelayCommand(() => _owner.RemoveParameter(this));
    }

    public ICommand RemoveCommand { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (!Set(ref _name, value))
                return;
            _owner.CommitParameters();
        }
    }

    public string Value
    {
        get => _value;
        set
        {
            if (!Set(ref _value, value))
                return;
            _owner.CommitParameters();
        }
    }

    public ParameterEntry ToEntry() => new() { Name = Name, Value = Value };
}

public sealed class FieldViewModel : ViewModelBase
{
    private readonly SettingsController _controller;
    private readonly SettingsViewModel _owner;
    private bool _syncing;

    public FieldViewModel(FieldDefinition definition, SettingsController controller, SettingsViewModel owner)
    {
        Definition = definition;
        _controller = controller;
        _owner = owner;
        CheckOptions = new ObservableCollection<CheckOptionViewModel>();
        Parameters = new ObservableCollection<ParameterRowViewModel>();
        Command = new RelayCommand(() => _owner.RunCommand(definition.Key), () => IsEnabled);
        AddParameterCommand = new RelayCommand(() => AddParameter(), () => IsEnabled);
        Refresh();
    }

    public FieldDefinition Definition { get; }
    public string Key => Definition.Key;
    public string Label => Definition.Label;
    public string? Hint => Definition.CommandHint;
    public ControlKind Control => Definition.Control;
    public IReadOnlyList<string> Options => Definition.Options ?? [];
    public double Min => Definition.Min ?? 0;
    public double Max => Definition.Max ?? 100;
    public ObservableCollection<CheckOptionViewModel> CheckOptions { get; }
    public ObservableCollection<ParameterRowViewModel> Parameters { get; }
    public ICommand Command { get; }
    public ICommand AddParameterCommand { get; }

    public bool IsEnabled => _controller.IsEnabled(Key);

    public bool BoolValue
    {
        get => _controller.Working.GetBool(Key);
        set
        {
            if (_syncing)
                return;
            _controller.Set(Key, value);
        }
    }

    public double DoubleValue
    {
        get => _controller.Working.GetNumber(Key);
        set
        {
            if (_syncing)
                return;
            var clamped = Math.Clamp(value, Min, Max);
            if (Definition.Control == ControlKind.Slider || Definition.Increment >= 1)
                _controller.Set(Key, (int)Math.Round(clamped));
            else
                _controller.Set(Key, clamped);
        }
    }

    public int IntValue
    {
        get => _controller.Working.GetInt(Key);
        set
        {
            if (_syncing)
                return;
            var clamped = (int)Math.Clamp(value, Min, Max);
            _controller.Set(Key, clamped);
        }
    }

    public string StringValue
    {
        get => Control == ControlKind.Color || Control == ControlKind.Text || Control == ControlKind.Dropdown
            ? _controller.Working.GetString(Key)
            : _controller.Working.Get(Key)?.ToString() ?? "";
        set
        {
            if (_syncing)
                return;
            _controller.Set(Key, value ?? "");
        }
    }

    public void Refresh()
    {
        _syncing = true;
        try
        {
            if (Control == ControlKind.CheckList)
            {
                var selected = new HashSet<string>(_controller.Working.GetStringList(Key), StringComparer.Ordinal);
                if (CheckOptions.Count == 0)
                {
                    foreach (var option in Options)
                        CheckOptions.Add(new CheckOptionViewModel(this, option, selected.Contains(option)));
                }
                else
                {
                    foreach (var option in CheckOptions)
                        option.SetCheckedSilent(selected.Contains(option.Name));
                }
            }

            if (Control == ControlKind.ParameterList)
            {
                Parameters.Clear();
                foreach (var entry in _controller.Working.GetParameters(Key))
                    Parameters.Add(new ParameterRowViewModel(this, entry));
            }

            Raise(nameof(BoolValue));
            Raise(nameof(DoubleValue));
            Raise(nameof(IntValue));
            Raise(nameof(StringValue));
            Raise(nameof(IsEnabled));
        }
        finally
        {
            _syncing = false;
        }
    }

    public void CommitCheckList()
    {
        if (_syncing)
            return;
        var selected = CheckOptions.Where(o => o.IsChecked).Select(o => o.Name).ToList();
        _controller.Set(Key, selected);
    }

    public void CommitParameters()
    {
        if (_syncing)
            return;
        _controller.Set(Key, Parameters.Select(p => p.ToEntry()).ToList());
    }

    public void AddParameter()
    {
        Parameters.Add(new ParameterRowViewModel(this, new ParameterEntry { Name = "Param", Value = "0" }));
        CommitParameters();
    }

    public void RemoveParameter(ParameterRowViewModel row)
    {
        Parameters.Remove(row);
        CommitParameters();
    }
}

