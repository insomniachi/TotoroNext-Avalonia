using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class AnimeProgressBehavior : TrackingBoundAnimeCardOverlayBehavior<Border>
{
    protected override Border CreateControl(AnimeModel anime)
    {
        return new Border()
               .Padding(8)
               .Margin(GetMarginForBottomLeftPlacement(8))
               .HorizontalAlignment(HorizontalAlignment.Left)
               .VerticalAlignment(VerticalAlignment.Bottom)
               .Background(new SolidColorBrush(Colors.Black, 0.7))
               .CornerRadius(5)
               .Child(new TextBlock()
                      .Foreground(Brushes.AntiqueWhite)
                      .FontWeight(FontWeight.Bold)
                      .Text($"{anime.Tracking?.WatchedEpisodes}/{anime.TotalEpisodes}"));
    }

    protected override bool CanCreate(AnimeModel anime)
    {
        return anime is
        {
            TotalEpisodes: not null,
            Tracking:
            {
                WatchedEpisodes: > 0,
                Status: not ListItemStatus.Completed
            }
        };
    }
}