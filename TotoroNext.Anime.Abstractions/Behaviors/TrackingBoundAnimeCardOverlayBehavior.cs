using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public abstract class TrackingBoundAnimeCardOverlayBehavior<TOverlay> : AnimeCardOverlayBehavior<TOverlay>
    where TOverlay : Control
{
    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .SelectMany(x => x.WhenAnyValue(y => y.Tracking).WhereNotNull())
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(tracking =>
                        {
                            EnsureControl();
                            UpdateControl(tracking);
                        })
                        .DisposeWith(Disposables);
    }

    protected abstract void UpdateControl(Tracking tracking);
}