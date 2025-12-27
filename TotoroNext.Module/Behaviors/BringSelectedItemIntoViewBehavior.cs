using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Xaml.Interactivity;

namespace TotoroNext.Module.Behaviors;

public class BringSelectedItemIntoViewBehavior : Behavior<SelectingItemsControl>
{
    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.SelectionChanged += OnSelectedItemChanged;
    }

    protected override void OnDetachedFromVisualTree()
    {
        AssociatedObject?.SelectionChanged -= OnSelectedItemChanged;
    }

    private static void OnSelectedItemChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not SelectingItemsControl lb)
        {
            return;
        }

        if (e.AddedItems is not { Count: 1 })
        {
            return;
        }

        if (e.AddedItems[0] is not { } item)
        {
            return;
        }

        lb.ScrollIntoView(item);
    }
}