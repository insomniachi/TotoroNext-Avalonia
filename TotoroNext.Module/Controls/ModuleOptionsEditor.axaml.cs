using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace TotoroNext.Module.Controls;

public partial class ModuleOptionsEditor : UserControl
{
    public static readonly StyledProperty<List<ModuleOptionItem>> OptionsProperty =
        AvaloniaProperty.Register<ModuleOptionsEditor, List<ModuleOptionItem>>(nameof(Options));

    public List<ModuleOptionItem> Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public ModuleOptionsEditor()
    {
        InitializeComponent();
    }
}