using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public abstract class AnimeBoundCardOverlayBehavior<TOverlay> : AnimeCardOverlayBehavior<TOverlay>
    where TOverlay : Control
{
    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .WhereNotNull()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(anime =>
                        {
                            RemoveControl();
                            EnsureControl();
                            UpdateControl(anime);
                        })
                        .DisposeWith(Disposables);
    }

    protected abstract void UpdateControl(AnimeModel anime);
}