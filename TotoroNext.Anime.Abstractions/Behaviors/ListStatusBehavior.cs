using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class ListStatusBehavior : TrackingBoundAnimeCardOverlayBehavior<Border>
{
    protected override Border CreateControl(AnimeModel anime)
    {
        return new Border()
               .Padding(5)
               .Margin(new Thickness(0, 3.5, 5, 0))
               .CornerRadius(15)
               .Width(50)
               .BorderThickness(1)
               .BorderBrush(Brushes.Black)
               .HorizontalAlignment(HorizontalAlignment.Right)
               .VerticalAlignment(VerticalAlignment.Top)
               .Background(GetBackgroundBrush(anime.Tracking))
               .Child(new Viewbox()
                      .Width(15)
                      .Height(15)
                      .Child(GetIcon(anime.Tracking)));
    }

    private static PathIcon GetIcon(Tracking? tracking)
    {
        var icon = tracking?.Status switch
        {
            ListItemStatus.Completed => IconRegistry.GetPathIcon(CommonIcons.Check),
            ListItemStatus.Watching => IconRegistry.GetPathIcon(CommonIcons.HourglassFill),
            ListItemStatus.OnHold => IconRegistry.GetPathIcon(CommonIcons.DebugPause),
            ListItemStatus.Dropped => IconRegistry.GetPathIcon(CommonIcons.MemoryTrash),
            ListItemStatus.PlanToWatch => IconRegistry.GetPathIcon(CommonIcons.Bookmark),
            _ => new PathIcon()
        };
        icon.Foreground = Brushes.Black;
        return icon;
    }

    private static IImmutableSolidColorBrush GetBackgroundBrush(Tracking? tracking)
    {
        return tracking?.Status switch
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