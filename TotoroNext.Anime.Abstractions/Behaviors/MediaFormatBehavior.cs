using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class MediaFormatBehavior : AnimeBoundCardOverlayBehavior<Border>
{
    protected override Border CreateControl(AnimeModel anime)
    {
        return new Border()
               .Background(Brushes.GreenYellow)
               .BorderBrush(Brushes.Black)
               .BorderThickness(1)
               .HorizontalAlignment(HorizontalAlignment.Left)
               .VerticalAlignment(VerticalAlignment.Top)
               .CornerRadius(20)
               .Padding(12, 5)
               .MinWidth(50)
               .Margin(5, 3.5, 0)
               .Child(new TextBlock()
                      .HorizontalAlignment(HorizontalAlignment.Center)
                      .VerticalAlignment(VerticalAlignment.Center)
                      .FontWeight(FontWeight.Bold)
                      .Foreground(Brushes.Black)
                      .FontSize(12)
                      .Text(anime.MediaFormat.ToString().ToUpper()));
    }
}