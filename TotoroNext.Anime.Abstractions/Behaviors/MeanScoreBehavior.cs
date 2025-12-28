using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using IconPacks.Avalonia;
using IconPacks.Avalonia.MaterialDesign;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class MeanScoreBehavior : AnimeBoundCardOverlayBehavior<Border>
{
    protected override Border CreateControl(AnimeModel anime)
    {
        return new Border()
               .Padding(8)
               .Margin(GetMarginForBottomRightPlacement(8))
               .HorizontalAlignment(HorizontalAlignment.Right)
               .VerticalAlignment(VerticalAlignment.Bottom)
               .Background(new SolidColorBrush(Colors.Black, 0.7))
               .CornerRadius(5)
               .Child(new StackPanel()
                      .Spacing(4)
                      .Orientation(Orientation.Horizontal)
                      .Children(new PackIconControl { Kind = PackIconMaterialDesignKind.Star }
                                    .Height(12)
                                    .Width(12)
                                    .VerticalAlignment(VerticalAlignment.Center),
                                new TextBlock()
                                    .Foreground(Brushes.AntiqueWhite)
                                    .FontWeight(FontWeight.Bold)
                                    .Text($"{anime.MeanScore}")));
    }
}