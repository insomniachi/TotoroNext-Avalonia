using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public abstract class AnimeCardOverlayBehavior<TOverlay> : Behavior<AnimeCard>, IAnimeCardOverlayBehavior
    where TOverlay : Control
{
    protected TOverlay? Control;
    // ReSharper disable once CollectionNeverUpdated.Global
    protected readonly CompositeDisposable Disposables = new();
    
    public bool HideOnPointerOver { get; set; } = true;

    public void OnPointerEntered()
    {
        if (!HideOnPointerOver)
        {
            return;
        }
        
        Control?.IsVisible = false;
    }

    public void OnPointerExited()
    {
        if (!HideOnPointerOver)
        {
            return;
        }
        
        Control?.IsVisible = true;
    }

    protected override void OnDetachedFromVisualTree()
    {
        RemoveControl();
        Disposables.Dispose();
    }

    protected void EnsureControl()
    {
        if (Control is not null)
        {
            return;
        }

        Control = CreateControl();
        AssociatedObject?.ImageContainer.Children.Add(Control);
    }

    protected void RemoveControl()
    {
        if (Control is null)
        {
            return;
        }

        AssociatedObject?.ImageContainer.Children.Remove(Control);
        Control = null;
    }

    protected abstract TOverlay CreateControl();
}