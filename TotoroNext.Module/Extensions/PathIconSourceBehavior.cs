using Avalonia;
using FluentAvalonia.UI.Controls;

namespace TotoroNext.Module.Extensions;

public class PathIconSourceBehavior
{
    public static readonly AttachedProperty<string?> IconKeyProperty =
        AvaloniaProperty.RegisterAttached<FASettingsExpander, string?>(
                                                                       "IconKey",
                                                                       typeof(PathIconSourceBehavior),
                                                                       null,
                                                                       false);

    static PathIconSourceBehavior()
    {
        IconKeyProperty.Changed.AddClassHandler<FASettingsExpander>(OnIconKeyChanged);
    }

    private static void OnIconKeyChanged(FASettingsExpander control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is string keyString)
        {
            var iconSource = IconRegistry.GetPathIconSource(keyString);
            if (iconSource != null)
            {
                control.IconSource = iconSource;
            }
        }
    }

    public static string? GetIconKey(FASettingsExpander control)
    {
        return control.GetValue(IconKeyProperty);
    }

    public static void SetIconKey(FASettingsExpander control, string? value)
    {
        control.SetValue(IconKeyProperty, value);
    }
}