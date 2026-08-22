using Avalonia;
using Avalonia.Controls;

namespace TotoroNext.Module.Controls;

public partial class DataContainerEditor : UserControl
{
    public static readonly StyledProperty<List<DataContainerProperty>> OptionsProperty =
        AvaloniaProperty.Register<DataContainerEditor, List<DataContainerProperty>>(nameof(Options));

    public DataContainerEditor()
    {
        InitializeComponent();
    }

    public List<DataContainerProperty> Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }
}