using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Behaviors;

public class AiringStatusBehavior : Behavior<AnimeCard>
{
    private static readonly SolidColorBrush AiringBrush = new(Colors.LimeGreen);
    private static readonly SolidColorBrush FinishedBrush = new(Colors.MediumSlateBlue);
    private static readonly SolidColorBrush NotYetBrush = new(Colors.LightSlateGray);
    private static readonly SolidColorBrush OtherBrush = new(Colors.Transparent);

    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        AssociatedObject.StatusBorder.BorderBrush = ToBrush(AssociatedObject.Anime);
    }


    private static SolidColorBrush ToBrush(AnimeModel anime)
    {
        return anime.AiringStatus switch
        {
            AiringStatus.CurrentlyAiring => AiringBrush,
            AiringStatus.FinishedAiring => FinishedBrush,
            AiringStatus.NotYetAired => NotYetBrush,
            _ => OtherBrush
        };
    }
}