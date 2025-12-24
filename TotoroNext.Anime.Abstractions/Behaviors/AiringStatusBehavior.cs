using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class AiringStatusBehavior : Behavior<AnimeCard>
{
    private readonly CompositeDisposable _disposables = new();

    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .WhereNotNull()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => AssociatedObject.StatusBorder.BorderBrush = ToBrush(AssociatedObject.Anime))
                        .DisposeWith(_disposables);
    }

    protected override void OnDetachedFromVisualTree()
    {
        _disposables.Dispose();
    }

    private static IImmutableBrush ToBrush(AnimeModel anime)
    {
        return anime.AiringStatus switch
        {
            AiringStatus.CurrentlyAiring => Brushes.LimeGreen,
            AiringStatus.FinishedAiring => Brushes.MediumSlateBlue,
            AiringStatus.NotYetAired => Brushes.LightSlateGray,
            _ => Brushes.Transparent
        };
    }
}