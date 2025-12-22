using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
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
        if (AssociatedObject is null)
        {
            return;
        }

        if (!AssociatedObject.ShowCompletedStatus)
        {
            return;
        }

        AssociatedObject.GetObservable(AnimeCard.AnimeProperty)
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
        var icon = new PackIconControl
        {
            Name = "IconControl",
            Width = 15,
            Height = 15,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.Black
        };

        var border = new Border
        {
            Name = "IconControlBorder",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 3.5, 5, 0),
            Width = 50,
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(15),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Child = icon
        };

        return border;
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