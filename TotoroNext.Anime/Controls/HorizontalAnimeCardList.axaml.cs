using System.Diagnostics;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Controls;

public partial class HorizontalAnimeCardList : UserControl
{
    public static readonly StyledProperty<List<AnimeModel>> AnimeProperty =
        AvaloniaProperty.Register<HorizontalAnimeCardList, List<AnimeModel>>(nameof(Anime));

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<HorizontalAnimeCardList, string>(nameof(Title));

    public static readonly StyledProperty<Func<Task<List<AnimeModel>>>?> AsyncPopulatorProperty =
        AvaloniaProperty.Register<HorizontalAnimeCardList, Func<Task<List<AnimeModel>>>?>(nameof(AsyncPopulator));

    public HorizontalAnimeCardList()
    {
        InitializeComponent();
        this.GetObservable(AsyncPopulatorProperty)
            .WhereNotNull()
            .Select(_ => Observable.FromAsync(TryPopulate))
            .Switch()
            .Subscribe();
    }

    public static IEnumerable<int> SkeletonItems => Enumerable.Range(0, 10);

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

    public Func<Task<List<AnimeModel>>>? AsyncPopulator
    {
        get => GetValue(AsyncPopulatorProperty);
        set => SetValue(AsyncPopulatorProperty, value);
    }

    private void ScrollLeft(object? sender, RoutedEventArgs e)
    {
        if (Anime.Count == 0)
        {
            return;
        }

        const int viewPortLeft = 0;

        for (var i = Anime.Count; i >= 0; i--)
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
        if (Anime.Count == 0)
        {
            return;
        }

        var viewportRight = Scroller.Viewport.Width;

        for (var i = 0; i < Anime.Count; i++)
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
            
            if(itemRight < viewportRight)
            {
                continue;
            }
            
            var offset = 0d;
            if (itemLeft < viewportRight && itemRight > viewportRight)
            {
                offset = itemRight - viewportRight;
            }

            _ = SmoothScrollToAsync(Scroller, Scroller.Offset.X + itemLeft + offset);
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
        const int durationMs = 300; // adjust for speed

        var stopwatch = new Stopwatch();
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

    private async Task TryPopulate()
    {
        if (AsyncPopulator is null)
        {
            return;
        }

        try
        {
            Dispatcher.UIThread.Invoke(() => { ItemsHostSkeleton.IsVisible = true; });

            var list = await AsyncPopulator.Invoke();

            if (list.Count == 0)
            {
                return;
            }

            Dispatcher.UIThread.Invoke(() => { Anime = list; });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            Dispatcher.UIThread.Invoke(() => { ItemsHostSkeleton.IsVisible = false; });
        }
    }
}