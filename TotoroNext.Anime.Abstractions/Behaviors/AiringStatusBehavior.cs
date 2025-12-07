using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class AiringStatusBehavior : Behavior<AnimeCard>, IVirtualizingBehavior<AnimeCard>
{
    private static readonly SolidColorBrush AiringBrush = new(Colors.LimeGreen);
    private static readonly SolidColorBrush FinishedBrush = new(Colors.MediumSlateBlue);
    private static readonly SolidColorBrush NotYetBrush = new(Colors.LightSlateGray);
    private static readonly SolidColorBrush OtherBrush = new(Colors.Transparent);

    public void Update(AnimeCard card)
    {
        card.StatusBorder.BorderBrush = ToBrush(card.Anime);
    }

    protected override void OnAttachedToVisualTree()
    {
        if (AssociatedObject is null)
        {
            return;
        }

        Update(AssociatedObject);
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