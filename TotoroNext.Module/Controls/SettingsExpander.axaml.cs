using Avalonia;
using Avalonia.Controls;

namespace TotoroNext.Module.Controls;

public partial class SettingsExpander : UserControl
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<SettingsExpander, string>(nameof(Header));

    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<SettingsExpander, string>(nameof(Description));

    public static readonly StyledProperty<object> EditorProperty =
        AvaloniaProperty.Register<SettingsExpander, object>(nameof(Editor));

    public static readonly StyledProperty<string> IconKeyProperty =
        AvaloniaProperty.Register<SettingsExpander, string>(nameof(IconKey));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<SettingsExpander, bool>(nameof(IsExpanded));

    public SettingsExpander()
    {
        InitializeComponent();
    }

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string IconKey
    {
        get => GetValue(IconKeyProperty);
        set => SetValue(IconKeyProperty, value);
    }

    public object Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }
}