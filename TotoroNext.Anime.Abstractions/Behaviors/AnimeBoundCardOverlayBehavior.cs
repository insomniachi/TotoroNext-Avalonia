using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public abstract class AnimeBoundCardOverlayBehavior<TOverlay> : AnimeCardOverlayBehavior<TOverlay>
    where TOverlay : Control
{
    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .WhereNotNull()
                        .ObserveOn(RxSchedulers.MainThreadScheduler)
                        .Subscribe(anime =>
                        {
                            RemoveControl();
                            EnsureControl(anime);
                        })
                        .DisposeWith(Disposables);
    }
}