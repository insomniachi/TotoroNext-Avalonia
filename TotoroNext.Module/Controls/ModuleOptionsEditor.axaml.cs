using Avalonia;
using Avalonia.Controls;

namespace TotoroNext.Module.Controls;

public partial class ModuleOptionsEditor : UserControl
{
    public static readonly StyledProperty<List<ModuleOptionItem>> OptionsProperty =
        AvaloniaProperty.Register<ModuleOptionsEditor, List<ModuleOptionItem>>(nameof(Options));

    public ModuleOptionsEditor()
    {
        InitializeComponent();
    }

    public List<ModuleOptionItem> Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }
}