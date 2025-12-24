using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using IconPacks.Avalonia;
using IconPacks.Avalonia.Codicons;
using IconPacks.Avalonia.MaterialDesign;
using IconPacks.Avalonia.MemoryIcons;
using IconPacks.Avalonia.PhosphorIcons;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class ListStatusBehavior : Behavior<AnimeCard>, IControlAttachingBehavior
{
    private readonly CompositeDisposable _disposables = new();
    private Border? _container;

    public void OnHoverEntered()
    {
        _container?.IsVisible = false;
    }

    public void OnHoverExited()
    {
        _container?.IsVisible = true;
    }

    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .SelectMany(x => x.WhenAnyValue(y => y.Tracking).WhereNotNull())
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(tracking =>
                        {
                            EnsureControl();
                            Update(tracking);
                        })
                        .DisposeWith(_disposables);
    }

    protected override void OnDetachedFromVisualTree()
    {
        if (_container is not null)
        {
            AssociatedObject?.ImageContainer.Children.Remove(_container);
            _container = null;
        }

        _disposables.Dispose();
    }

    private void EnsureControl()
    {
        if (_container is not null)
        {
            return;
        }

        _container = CreateControl();
        AssociatedObject?.ImageContainer.Children.Add(_container);
    }

    private static Enum? GetIcon(Tracking tracking)
    {
        return tracking.Status switch
        {
            ListItemStatus.Completed => PackIconMaterialDesignKind.Check,
            ListItemStatus.Watching => PackIconPhosphorIconsKind.HourglassHighFill,
            ListItemStatus.OnHold => PackIconCodiconsKind.DebugPause,
            ListItemStatus.Dropped => PackIconMemoryIconsKind.MemoryTrash,
            ListItemStatus.PlanToWatch => PackIconPhosphorIconsKind.HourglassHighFill,
            _ => null
        };
    }

    private static IImmutableSolidColorBrush GetBackgroundBrush(Tracking tracking)
    {
        return tracking.Status switch
        {
            ListItemStatus.Completed => Brushes.LawnGreen,
            ListItemStatus.Watching => Brushes.LawnGreen,
            ListItemStatus.OnHold => Brushes.Orange,
            ListItemStatus.PlanToWatch => Brushes.Orange,
            ListItemStatus.Dropped => Brushes.Red,
            _ => Brushes.Transparent
        };
    }

    private static Border CreateControl()
    {
        return new Border()
               .Padding(5)
               .Margin(0, 3.5, 5)
               .Width(50)
               .BorderThickness(1)
               .BorderBrush(Brushes.Black)
               .HorizontalAlignment(HorizontalAlignment.Right)
               .VerticalAlignment(VerticalAlignment.Top)
               .Child(new PackIconControl()
                      .Width(15)
                      .Height(15)
                      .HorizontalAlignment(HorizontalAlignment.Center)
                      .Foreground(Brushes.Black));
    }

    private void Update(Tracking tracking)
    {
        if (_container is null)
        {
            return;
        }

        _container.Background = GetBackgroundBrush(tracking);
        ((PackIconControl)_container.Child!).Kind = GetIcon(tracking);
    }
}