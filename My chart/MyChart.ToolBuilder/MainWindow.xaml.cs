using System.Windows;
using MyChart.Core.UI.ToolBuilder;
using MyChart.ToolBuilder.ViewModels;

namespace MyChart.ToolBuilder;

public partial class MainWindow : Window
{
    private readonly ToolDefinitionFormViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();
        RefreshPreview();
    }

    private void SyncModel()
    {
        _vm.Name = NameBox.Text.Trim();
        if (int.TryParse(AnchorsBox.Text, out var a))
            _vm.Model.Anchors = a;
        _vm.Model.Workflow = (WorkflowBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "Click";
    }

    private void RefreshPreview()
    {
        SyncModel();
        _vm.Model.Validate();
        ErrorText.Text = _vm.LastError ?? "";
        JsonPreview.Text = _vm.Model.BuildDefinitionJson();
        ShapesList.Items.Clear();
        foreach (var s in _vm.Model.Shapes)
            ShapesList.Items.Add($"{s.Kind}  {s.FromAnchor}->{s.ToAnchor}");
    }

    private void AddLine_OnClick(object sender, RoutedEventArgs e)
    {
        SyncModel();
        _vm.Model.AddShape("Line");
        RefreshPreview();
    }

    private void RemoveShape_OnClick(object sender, RoutedEventArgs e)
    {
        SyncModel();
        _vm.Model.RemoveShape(ShapesList.SelectedIndex);
        RefreshPreview();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        SyncModel();
        if (!_vm.SaveCommand())
        {
            ErrorText.Text = _vm.LastError ?? "Cannot save.";
            MessageBox.Show(ErrorText.Text, "Save blocked", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        MessageBox.Show("Definition is valid. Disk write is completed in PG6.03 Save pipeline.", "Valid",
            MessageBoxButton.OK, MessageBoxImage.Information);
        RefreshPreview();
    }
}
