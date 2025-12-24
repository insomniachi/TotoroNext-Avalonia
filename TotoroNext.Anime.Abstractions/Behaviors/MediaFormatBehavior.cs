using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class MediaFormatBehavior : AnimeBoundCardOverlayBehavior<Border>
{
    protected override Border CreateControl()
    {
        return new Border()
               .Background(Brushes.GreenYellow)
               .BorderBrush(Brushes.Black)
               .BorderThickness(1)
               .HorizontalAlignment(HorizontalAlignment.Left)
               .VerticalAlignment(VerticalAlignment.Top)
               .CornerRadius(20)
               .Padding(8)
               .Margin(8)
               .Child(new TextBlock()
                      .HorizontalAlignment(HorizontalAlignment.Center)
                      .FontWeight(FontWeight.SemiBold)
                      .Foreground(Brushes.Black)
                      .FontSize(13));
    }

    protected override void UpdateControl(AnimeModel anime)
    {
        (Control?.Child as TextBlock)!.Text = anime.MediaFormat.ToString().ToUpper();
    }
}