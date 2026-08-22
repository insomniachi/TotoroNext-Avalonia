using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

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

public class DataContainerPropertyTemplateSelector : IDataTemplate
{
    public IDataTemplate? TextTemplate { get; set; }
    public IDataTemplate? ComboBoxTemplate { get; set; }
    public IDataTemplate? ToggleSwitchTemplate { get; set; }
    public IDataTemplate? FileBrowserTemplate { get; set; }
    public IDataTemplate? NumberBoxTemplate { get; set; }
    
    public Control? Build(object? param)
    {
        if (param is not DataContainerProperty item)
        {
            return null;
        }

        return item.EditorType switch
        {
            SpecialEditorType.TextBox => TextTemplate?.Build(param),
            SpecialEditorType.ComboBox => ComboBoxTemplate?.Build(param),
            SpecialEditorType.ToggleSwitch => ToggleSwitchTemplate?.Build(param),
            SpecialEditorType.FileBrowser => FileBrowserTemplate?.Build(param),
            SpecialEditorType.NumberBox => NumberBoxTemplate?.Build(param),
            _ => throw new UnreachableException()
        };
    }

    public bool Match(object? data) => data is DataContainerProperty;
}