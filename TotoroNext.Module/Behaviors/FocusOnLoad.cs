using Avalonia;
using Avalonia.Controls;

namespace TotoroNext.Module.Behaviors;

public static class FocusOnLoad
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(FocusOnLoad));

    static FocusOnLoad()
    {
        IsEnabledProperty.Changed.Subscribe(args =>
        {
            if (args is not { Sender: Control control, NewValue.Value: true })
            {
                return;
            }

            control.AttachedToVisualTree += (_, _) => control.Focus();
        });
    }

    public static void SetIsEnabled(AvaloniaObject element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    public static bool GetIsEnabled(AvaloniaObject element)
    {
        return element.GetValue(IsEnabledProperty);
    }
}