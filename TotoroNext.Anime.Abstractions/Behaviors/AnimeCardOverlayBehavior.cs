using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using Microsoft.Playwright;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Models;

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

    protected void EnsureControl(AnimeModel anime)
    {
        if (Control is not null)
        {
            return;
        }

        if (!CanCreate(anime))
        {
            return;
        }

        Control = CreateControl(anime);
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

    protected virtual bool CanCreate(AnimeModel anime) => true;

    protected abstract TOverlay CreateControl(AnimeModel anime);

    protected Thickness GetMarginForBottomPlacement(double extra)
    {
        return new Thickness(extra, 0, 0, (AssociatedObject?.StatusBorder.Height ?? 0) + extra);
    }
}