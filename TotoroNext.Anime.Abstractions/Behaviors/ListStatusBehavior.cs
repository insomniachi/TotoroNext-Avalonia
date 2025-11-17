using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using IconPacks.Avalonia.Codicons;
using IconPacks.Avalonia.MaterialDesign;
using IconPacks.Avalonia.MemoryIcons;
using IconPacks.Avalonia.PhosphorIcons;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class ListStatusBehavior : Behavior<AnimeCard>
{
    private static readonly SolidColorBrush GreenColor = new(Colors.LawnGreen);
    private static readonly SolidColorBrush OrangeColor = new(Colors.Orange);
    private static readonly SolidColorBrush RedColor = new(Colors.Red);
    private static readonly SolidColorBrush TransparentColor = new(Colors.Transparent);

    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject?.ShowCompletedStatus == false)
        {
            return;
        }

        if (AssociatedObject?.Anime is not { } anime)
        {
            return;
        }

        if (anime.Tracking is null)
        {
            return;
        }

        AssociatedObject.CompletedCheckMark.Background = GetBackgroundBrush(anime.Tracking);
        AssociatedObject.IconControl.Kind = GetIcon(anime.Tracking);
        AssociatedObject.CompletedCheckMark.IsVisible = true;
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

    private static SolidColorBrush GetBackgroundBrush(Tracking tracking)
    {
        return tracking.Status switch
        {
            ListItemStatus.Completed => GreenColor,
            ListItemStatus.Watching => GreenColor,
            ListItemStatus.OnHold => OrangeColor,
            ListItemStatus.PlanToWatch => OrangeColor,
            ListItemStatus.Dropped => RedColor,
            _ => TransparentColor
        };
    }
}