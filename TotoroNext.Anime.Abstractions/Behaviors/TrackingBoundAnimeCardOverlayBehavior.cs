using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public abstract class TrackingBoundAnimeCardOverlayBehavior<TOverlay> : AnimeCardOverlayBehavior<TOverlay>
    where TOverlay : Control
{
    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .SelectMany(x => x.WhenAnyValue(y => y.Tracking).WhereNotNull())
                        .ObserveOn(RxSchedulers.MainThreadScheduler)
                        .Subscribe(_ =>
                        {
                            RemoveControl();
                            EnsureControl(AssociatedObject.Anime);
                        })
                        .DisposeWith(Disposables);
    }
}