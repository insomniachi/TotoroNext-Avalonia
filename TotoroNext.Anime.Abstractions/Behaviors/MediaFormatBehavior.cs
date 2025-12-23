using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Xaml.Interactivity;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Abstractions.Behaviors;

public class MediaFormatBehavior : Behavior<AnimeCard>, IControlAttachingBehavior
{
    private readonly CompositeDisposable _disposable = new();
    private Border? _control;

    public void OnHoverEntered()
    {
        _control?.IsVisible = false;
    }

    public void OnHoverExited()
    {
        _control?.IsVisible = true;
    }

    protected override void OnAttachedToVisualTree()
    {
        AssociatedObject?.GetObservable(AnimeCard.AnimeProperty)
                        .WhereNotNull()
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Subscribe(anime =>
                        {
                            RemoveControl();
                            EnsureControl();
                            (_control?.Child as TextBlock)!.Text = anime.MediaFormat.ToString().ToUpper();
                        })
                        .DisposeWith(_disposable);
    }

    protected override void OnDetachedFromVisualTree()
    {
        RemoveControl();
        _disposable.Dispose();
    }

    private void RemoveControl()
    {
        if (_control is null)
        {
            return;
        }

        AssociatedObject?.ImageContainer.Children.Remove(_control);
        _control = null;
    }

    private void EnsureControl()
    {
        if (_control is not null)
        {
            return;
        }

        _control = CreateControl();
        AssociatedObject?.ImageContainer.Children.Add(_control);
    }

    private static Border CreateControl()
    {
        return new Border
        {
            Background = Brushes.GreenYellow,
            BorderBrush = Brushes.Black,
            BorderThickness =  new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(8),
            Margin = new Thickness(8),
            Child = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.Black,
                FontSize = 13,
            }
        };
    }
}