using Avalonia.Xaml.Interactivity;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class CompletedCheckMarkBehavior : Behavior<AnimeCard>
{
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

        if (anime.Tracking?.Status != ListItemStatus.Completed)
        {
            return;
        }

        AssociatedObject.CompletedCheckMark.IsVisible = true;
    }
}