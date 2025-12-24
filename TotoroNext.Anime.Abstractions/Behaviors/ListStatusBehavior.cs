using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using IconPacks.Avalonia;
using IconPacks.Avalonia.Codicons;
using IconPacks.Avalonia.MaterialDesign;
using IconPacks.Avalonia.MemoryIcons;
using IconPacks.Avalonia.PhosphorIcons;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class ListStatusBehavior : TrackingBoundAnimeCardOverlayBehavior<Border>
{
    protected override Border CreateControl()
    {
        return new Border()
               .Padding(5)
               .Margin(0, 3.5, 5)
               .CornerRadius(15)
               .Width(50)
               .BorderThickness(1)
               .BorderBrush(Brushes.Black)
               .HorizontalAlignment(HorizontalAlignment.Right)
               .VerticalAlignment(VerticalAlignment.Top)
               .Child(new PackIconControl()
                      .Width(15)
                      .Height(15)
                      .HorizontalAlignment(HorizontalAlignment.Center)
                      .Foreground(Brushes.Black));
    }

    protected override void UpdateControl(Tracking tracking)
    {
        if (Control is null)
        {
            return;
        }

        Control.Background = GetBackgroundBrush(tracking);
        ((PackIconControl)Control.Child!).Kind = GetIcon(tracking);
    }


    private static Enum? GetIcon(Tracking tracking)
    {
        return tracking.Status switch
        {
            ListItemStatus.Completed => PackIconMaterialDesignKind.Check,
            ListItemStatus.Watching => PackIconPhosphorIconsKind.HourglassHighFill,
            ListItemStatus.OnHold => PackIconCodiconsKind.DebugPause,
            ListItemStatus.Dropped => PackIconMemoryIconsKind.MemoryTrash,
            ListItemStatus.PlanToWatch => PackIconPhosphorIconsKind.HourglassHighFill,
            _ => null
        };
    }

    private static IImmutableSolidColorBrush GetBackgroundBrush(Tracking tracking)
    {
        return tracking.Status switch
        {
            ListItemStatus.Completed => Brushes.LawnGreen,
            ListItemStatus.Watching => Brushes.LawnGreen,
            ListItemStatus.OnHold => Brushes.Orange,
            ListItemStatus.PlanToWatch => Brushes.Orange,
            ListItemStatus.Dropped => Brushes.Red,
            _ => Brushes.Transparent
        };
    }
}