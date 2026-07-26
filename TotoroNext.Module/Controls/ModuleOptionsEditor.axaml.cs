using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

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

public class ModuleOptionItemTemplateSelector : IDataTemplate
{
    public IDataTemplate? TextTemplate { get; set; }
    public IDataTemplate? ComboBoxTemplate { get; set; }
    public IDataTemplate? ToggleSwitchTemplate { get; set; }
    public IDataTemplate? FileBrowserTemplate { get; set; }
    
    public Control? Build(object? param)
    {
        if (param is not ModuleOptionItem item)
        {
            return null;
        }

        return item.EditorType switch
        {
            SpecialEditorType.TextBox => TextTemplate?.Build(param),
            SpecialEditorType.ComboBox => ComboBoxTemplate?.Build(param),
            SpecialEditorType.ToggleSwitch => ToggleSwitchTemplate?.Build(param),
            SpecialEditorType.FileBrowser => FileBrowserTemplate?.Build(param),
            _ => throw new UnreachableException()
        };
    }

    public bool Match(object? data) => data is ModuleOptionItem;
}