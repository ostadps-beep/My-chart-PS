using System.Windows;
using System.Windows.Controls;
using ChartMy.Model;
using ChartMy.ViewModels;

namespace ChartMy.Views;

public sealed class FieldTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Dropdown { get; set; }
    public DataTemplate? Slider { get; set; }
    public DataTemplate? Numeric { get; set; }
    public DataTemplate? Toggle { get; set; }
    public DataTemplate? Color { get; set; }
    public DataTemplate? Text { get; set; }
    public DataTemplate? CheckList { get; set; }
    public DataTemplate? ParameterList { get; set; }
    public DataTemplate? Command { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is not FieldViewModel field)
            return base.SelectTemplate(item, container);

        return field.Control switch
        {
            ControlKind.Dropdown => Dropdown,
            ControlKind.Slider => Slider,
            ControlKind.Numeric => Numeric,
            ControlKind.Toggle => Toggle,
            ControlKind.Color => Color,
            ControlKind.Text => Text,
            ControlKind.CheckList => CheckList,
            ControlKind.ParameterList => ParameterList,
            ControlKind.Command => Command,
            _ => base.SelectTemplate(item, container)
        };
    }
}
