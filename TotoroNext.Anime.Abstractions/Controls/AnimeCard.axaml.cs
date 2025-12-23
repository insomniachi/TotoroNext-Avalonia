using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;
using TotoroNext.Anime.Abstractions.Behaviors;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Controls;

public partial class AnimeCard : UserControl
{
    public static readonly StyledProperty<AnimeModel> AnimeProperty =
        AvaloniaProperty.Register<AnimeCard, AnimeModel>(nameof(Anime));

    private CancellationTokenSource? _exitDelayToken;

    public AnimeCard()
    {
        InitializeComponent();
    }

    public AnimeModel Anime
    {
        get => GetValue(AnimeProperty);
        set => SetValue(AnimeProperty, value);
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        _exitDelayToken?.Cancel(); // Cancel any pending collapse

        foreach (var behavior in AvaloniaObject.GetBehaviors(this).OfType<IControlAttachingBehavior>())
        {
            behavior.OnHoverEntered();
        }
    }

    private async void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        try
        {
            if (_exitDelayToken is not null)
            {
                await _exitDelayToken.CancelAsync();
                _exitDelayToken.Dispose();
            }

            _exitDelayToken = new CancellationTokenSource();

            await Task.Delay(50, _exitDelayToken.Token); // Wait 300ms

            foreach (var behavior in AvaloniaObject.GetBehaviors(this).OfType<IControlAttachingBehavior>())
            {
                behavior.OnHoverExited();
            }
        }
        catch (Exception)
        {
            // Exit was canceled due to re-entry
        }
    }
}