using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class AiringStatusBehavior : Behavior<AnimeCard>
{
    private readonly CompositeDisposable _disposables = new();
    private static readonly SolidColorBrush AiringBrush = new(Colors.LimeGreen);
    private static readonly SolidColorBrush FinishedBrush = new(Colors.MediumSlateBlue);
    private static readonly SolidColorBrush NotYetBrush = new(Colors.LightSlateGray);
    private static readonly SolidColorBrush OtherBrush = new(Colors.Transparent);
    
    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .WhereNotNull()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(_ => AssociatedObject.StatusBorder.BorderBrush = ToBrush(AssociatedObject.Anime))
                        .DisposeWith(_disposables);
    }

    protected override void OnDetachedFromLogicalTree()
    {
        _disposables.Dispose();
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