using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Threading;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.Controls;

public partial class HorizontalAnimeCardList : UserControl
{
    public static readonly StyledProperty<List<AnimeModel>> AnimeProperty =
        AvaloniaProperty.Register<HorizontalAnimeCardList, List<AnimeModel>>(nameof(Anime));
    
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<HorizontalAnimeCardList, string>(nameof(Title));

    public HorizontalAnimeCardList()
    {
        InitializeComponent();
    }

    public List<AnimeModel> Anime
    {
        get => GetValue(AnimeProperty);
        set => SetValue(AnimeProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private void ScrollLeft(object? sender, RoutedEventArgs e)
    {
        var count = Anime.Count();
        if (count == 0)
        {
            return;
        }
        
        const int viewPortLeft = 0;
        
        for (var i = count; i >= 0; i--)
        {
            if (ItemsHost.ContainerFromIndex(i) is not ContentPresenter container)
            {
                continue;
            }

            var transform = container.TranslatePoint(new Point(0, 0), Scroller);
            if (!transform.HasValue)
            {
                continue;
            }

            var itemLeft = transform.Value.X;
            var itemRight = itemLeft + container.Bounds.Width;

            if (itemLeft > viewPortLeft)
            {
                continue;
            }
            
            var offset = 0d;
            if (itemLeft < viewPortLeft && itemRight > viewPortLeft)
            {
                offset = itemRight - viewPortLeft;
            }
            
            _ = SmoothScrollToAsync(Scroller, Scroller.Offset.X - Scroller.Viewport.Width + offset);
            break;

        }
    }

    private void ScrollRight(object? sender, RoutedEventArgs e)
    {
        var count = Anime.Count();
        if (count == 0)
        {
            return;
        }
        
        var viewportRight = Scroller.Viewport.Width;
        
        for (var i = 0; i < count; i++)
        {
            if (ItemsHost.ContainerFromIndex(i) is not ContentPresenter container)
            {
                continue;
            }

            var transform = container.TranslatePoint(new Point(0, 0), Scroller);
            if (!transform.HasValue)
            {
                continue;
            }

            var itemLeft = transform.Value.X;
            var itemRight = itemLeft + container.Bounds.Width;

            if (!(itemRight > viewportRight) || !(itemLeft < viewportRight))
            {
                continue;
            }

            _ = SmoothScrollToAsync(Scroller, Scroller.Offset.X + itemLeft);
            break;

        }
    }

    private void Scroller_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Horizontal offset
        var offsetX = Scroller.Offset.X;
        var maxX = Scroller.Extent.Width - Scroller.Viewport.Width;

        ScrollLeftButton.IsEnabled = offsetX > 1;
        ScrollRightButton.IsEnabled = offsetX < maxX - 1;
    }
    
    private static async Task SmoothScrollToAsync(ScrollViewer scroller, double targetX)
    {
        var startX = scroller.Offset.X;
        var distance = targetX - startX;
        var durationMs = 300; // adjust for speed

        var stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        var easing = new CubicEaseOut();

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            while (stopwatch.ElapsedMilliseconds < durationMs)
            {
                var progress = (double)stopwatch.ElapsedMilliseconds / durationMs;
                var eased = easing.Ease(progress); // nice easing
                var newX = startX + distance * eased;

                scroller.Offset = scroller.Offset.WithX(newX);
                await Task.Delay(16); // ~60fps
            }

            scroller.Offset = scroller.Offset.WithX(targetX); // snap to final
        });
    }
}