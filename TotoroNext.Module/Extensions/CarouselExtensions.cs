using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using FluentAvalonia.Core;
using ReactiveUI;

namespace TotoroNext.Module.Extensions;

public class CarouselExtensions
{
    public static readonly AttachedProperty<TimeSpan> ShuffleIntervalProperty =
        AvaloniaProperty.RegisterAttached<CarouselExtensions, Carousel, TimeSpan>("ShuffleInterval");

    static CarouselExtensions()
    {
        ShuffleIntervalProperty.Changed.AddClassHandler<Carousel>(OnIntervalChanged);
    }

    private static void OnIntervalChanged(Carousel sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not TimeSpan ts)
        {
            return;
        }

        var disposable = Observable.Timer(ts, ts)
                                   .ObserveOn(RxApp.MainThreadScheduler)
                                   .Select(_ => GetItemsCount(sender))
                                   .WhereNotNull()
                                   .Select(count => sender.SelectedIndex == count - 1 ? 0 : sender.SelectedIndex + 1)
                                   .Subscribe(index => sender.SelectedIndex = index);

        sender.Unloaded += (_, _) => disposable.Dispose();
    }

    public static void SetShuffleIntervalProperty(AvaloniaObject element, TimeSpan value)
    {
        element.SetValue(ShuffleIntervalProperty, value);
    }

    public static TimeSpan GetShuffleIntervalProperty(AvaloniaObject element)
    {
        return element.GetValue(ShuffleIntervalProperty);
    }

    private static int? GetItemsCount(Carousel control)
    {
        return control.ItemsSource?.Count();
    }
}