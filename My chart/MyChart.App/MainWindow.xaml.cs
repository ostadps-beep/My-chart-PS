using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using MyChart.Core.Models.Market;
using MyChart.Core.Services;
using MyChart.Core.UI.Icons;
using MyChart.Core.UI.Toolbar;
using MyChart.UI.Icons;

namespace MyChart.App;

/// <summary>
/// AFTER T7 (c) — Top/Left/Context toolbars wired to Core controllers (T6.03–T6.05).
/// Popups: 2-click rule (Open then Apply). Context bar only when selectionCount &gt; 0.
/// </summary>
public partial class MainWindow : Window
{
    private TopToolbarModel _top = TopToolbarModel.CreateDefault(settingsAvailable: true);
    private LeftToolbarModel _left = LeftToolbarModel.CreateDefault();
    private readonly TopToolbarController _topCtl = new();
    private readonly LeftToolbarController _leftCtl = new();
    private readonly ContextToolbarController _ctxCtl = new();
    private IconProvider? _icons;
    private int _selectionCount; // 0 until drawings selection exists (e)

    public MainWindow()
    {
        InitializeComponent();
        CompositionRoot.CreatePluginHost();
        _icons = CompositionRoot.Icons;
        WireSettingsToChart();
        BuildTopToolbar();
        BuildLeftToolbar();
        RefreshContextToolbar();
    }

    private void WireSettingsToChart()
    {
        var host = CompositionRoot.SettingsHost;
        if (host is null) return;

        host.Bridge.Changed += _ =>
        {
            Dispatcher.Invoke(() => Chart.ApplySettings(host.Bridge.Values));
        };
        Chart.ApplySettings(host.Bridge.Values);
    }

    private static string IconKeyFor(TopToolbarItemKind kind) => kind switch
    {
        TopToolbarItemKind.Symbol => "Icon.Symbol",
        TopToolbarItemKind.Timeframe => "Icon.Timeframe",
        TopToolbarItemKind.ChartType => "Icon.ChartType",
        TopToolbarItemKind.Indicators => "Icon.Indicators",
        TopToolbarItemKind.Templates => "Icon.Templates",
        TopToolbarItemKind.Layout => "Icon.Layout",
        TopToolbarItemKind.Settings => "Icon.Settings",
        _ => "Icon.Settings"
    };

    private UIElement CreateIcon(string iconKey, IconVisualState state = IconVisualState.Normal)
    {
        var view = new IconView { IconKey = iconKey, VisualState = state };
        if (_icons is not null)
            view.Attach(_icons, ThemeService.Dark);
        return view;
    }

    private void BuildTopToolbar()
    {
        _top = CompositionRoot.CreateTopToolbar(symbol: "EURUSD", timeframe: Timeframe.M1);
        TopToolbarHost.Items.Clear();
        foreach (var item in _top.Items)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(CreateIcon(IconKeyFor(item.Kind)));
            var text = item.SelectedValue is { Length: > 0 } sv ? sv : item.Label;
            panel.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                FontSize = 11,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            var btn = new Button
            {
                Content = panel,
                Style = (Style)FindResource("TbButton"),
                IsEnabled = item.IsEnabled,
                Tag = item,
                ToolTip = item.Label
            };
            btn.Click += OnTopToolbarClick;
            TopToolbarHost.Items.Add(btn);
        }
    }

    private void BuildLeftToolbar()
    {
        _left = LeftToolbarModel.CreateDefault(selectedItemId: _leftCtl.SelectedItemId);
        LeftToolbarHost.Items.Clear();
        foreach (var item in _left.Items)
        {
            var selected = item.ItemId == _leftCtl.SelectedItemId
                           || (item.Kind == LeftToolbarItemKind.Crosshair && _leftCtl.CrosshairActive);
            var icon = CreateIcon(
                string.IsNullOrWhiteSpace(item.IconKey) ? "Icon.Cursor" : item.IconKey,
                selected ? IconVisualState.Active : IconVisualState.Normal);

            var btn = new Button
            {
                Content = icon,
                Style = (Style)FindResource("LeftTbButton"),
                Tag = item,
                ToolTip = item.ItemId,
                Background = selected
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#505050")!)
                    : Brushes.Transparent
            };
            btn.Click += OnLeftToolbarClick;
            LeftToolbarHost.Items.Add(btn);
        }
    }

    private void RefreshContextToolbar()
    {
        // T6.05: hidden when selection empty
        var model = ContextToolbarModel.Create(
            selectionCount: _selectionCount,
            selectionLeftDip: 80,
            selectionTopDip: 120,
            selectionRightDip: 220,
            selectionBottomDip: 200,
            viewportTopDip: 0,
            viewportBottomDip: 600);

        var parent = ContextToolbarHost.Parent as FrameworkElement;
        if (parent is not null)
            parent.Visibility = model.IsVisible ? Visibility.Visible : Visibility.Collapsed;

        ContextToolbarHost.Items.Clear();
        if (!model.IsVisible)
            return;

        foreach (var item in model.Items)
        {
            var btn = new Button
            {
                Content = CreateIcon(item.IconKey),
                Style = (Style)FindResource("TbButton"),
                ToolTip = item.Action.ToString(),
                Tag = item
            };
            btn.Click += OnContextToolbarClick;
            ContextToolbarHost.Items.Add(btn);
        }
    }

    private void OnTopToolbarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not TopToolbarItem item)
            return;

        if (item.Kind == TopToolbarItemKind.Settings)
        {
            _topCtl.Close();
            OnSettingsClick(sender, e);
            return;
        }

        // T6.03: first click opens popup
        if (!_topCtl.Open(item))
        {
            StatusText.Text = "TopToolbar: disabled";
            return;
        }

        if (item.Choices.Count == 0)
        {
            StatusText.Text = $"TopToolbar open: {item.Kind} (no choices yet)";
            _topCtl.Close();
            return;
        }

        var menu = new ContextMenu
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")!),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333")!),
            Foreground = Brushes.White
        };

        foreach (var choice in item.Choices)
        {
            var mi = new MenuItem
            {
                Header = choice.IsQuick ? $"★ {choice.Label}" : choice.Label,
                Tag = (item, choice),
                Foreground = Brushes.White,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E")!)
            };
            mi.Click += OnTopToolbarChoice;
            menu.Items.Add(mi);
        }

        menu.PlacementTarget = btn;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
        btn.ContextMenu = menu;
        StatusText.Text = $"TopToolbar popup: {item.Kind}";
    }

    private void OnTopToolbarChoice(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: (TopToolbarItem item, ToolbarChoice choice) })
            return;

        // T6.03: second interaction applies
        if (_topCtl.Apply(item, choice.Value))
        {
            StatusText.Text = $"Applied {item.Kind} = {choice.Value}";
            // Refresh label on matching button
            BuildTopToolbar();
            // Re-stamp selected values for TF/ChartType/Layout after rebuild from defaults —
            // update status only; full ViewState wiring is later (d/e)
            if (item.Kind == TopToolbarItemKind.Timeframe)
                StatusText.Text = $"Timeframe → {choice.Value}";
        }
        else
            StatusText.Text = "Apply failed (open popup first)";
    }

    private void OnLeftToolbarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LeftToolbarItem item })
            return;

        if (!_leftCtl.Select(item))
            return;

        if (item.Kind == LeftToolbarItemKind.Crosshair)
        {
            StatusText.Text = _leftCtl.CrosshairActive
                ? "Crosshair ON"
                : "Crosshair OFF";
        }
        else
        {
            StatusText.Text = $"Tool: {item.ItemId}";
            // Drawing tools: context toolbar appears once selection exists (e).
            // For chrome VERIFY of strip, arm a provisional selection count when a draw tool is active.
            _selectionCount = item.Kind == LeftToolbarItemKind.DrawingTool ? 1 : 0;
            if (item.Kind == LeftToolbarItemKind.Cursor)
                _selectionCount = 0;
        }

        BuildLeftToolbar();
        RefreshContextToolbar();
    }

    private void OnContextToolbarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ContextToolbarItem item })
            return;

        if (_ctxCtl.TryIssue(item, _selectionCount))
            StatusText.Text = $"Context: {_ctxCtl.IssuedCommands.LastOrDefault()}";
        else
            StatusText.Text = "Context: no selection";
    }

    private void OnTitleBarDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { OnMaximize(sender, e); return; }
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var host = CompositionRoot.SettingsHost;
        if (host is null)
        {
            MessageBox.Show("SettingsHost is not initialized.", "MyChart");
            return;
        }
        try { host.ShowSettingsWindow(this); }
        catch (Exception ex) { MessageBox.Show("Failed to open Settings:\n" + ex.Message, "MyChart"); }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Fixtures"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "MyChart.Tests", "Fixtures")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "MyChart.Tests", "Fixtures")),
        };
        string? fixtures = candidates.FirstOrDefault(Directory.Exists);
        if (fixtures is null)
        {
            MessageBox.Show("Fixtures folder not found.");
            Chart.SetLoading(false);
            return;
        }
        try
        {
            await CompositionRoot.LoadFixturesAsync(Chart, fixtures);
            var host = CompositionRoot.SettingsHost;
            if (host is not null) Chart.ApplySettings(host.Bridge.Values);
            StatusText.Text = $"Ready · icons {_icons?.All.Count ?? 0}";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to load chart data:\n" + ex.Message);
            Chart.SetLoading(false);
        }
    }
}
