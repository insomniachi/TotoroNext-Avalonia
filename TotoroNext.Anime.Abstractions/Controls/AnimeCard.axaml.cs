using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.Abstractions.Controls;

public partial class AnimeCard : UserControl
{
    public static readonly StyledProperty<AnimeModel> AnimeProperty =
        AvaloniaProperty.Register<AnimeCard, AnimeModel>(nameof(Anime));

    public static readonly StyledProperty<ICommand> WatchCommandProperty =
        AvaloniaProperty.Register<AnimeCard, ICommand>(nameof(WatchCommand));

    public static readonly StyledProperty<ICommand> DetailsCommandProperty =
        AvaloniaProperty.Register<AnimeCard, ICommand>(nameof(DetailsCommand));

    public static readonly StyledProperty<bool> HasDetailsPaneProperty =
        AvaloniaProperty.Register<AnimeCard, bool>(nameof(HasDetailsPane));

    public static readonly StyledProperty<ICommand> SettingsCommandProperty =
        AvaloniaProperty.Register<AnimeCard, ICommand>(nameof(SettingsCommand));

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

    public ICommand WatchCommand
    {
        get => GetValue(WatchCommandProperty);
        set => SetValue(WatchCommandProperty, value);
    }

    public ICommand DetailsCommand
    {
        get => GetValue(DetailsCommandProperty);
        set => SetValue(DetailsCommandProperty, value);
    }

    public ICommand SettingsCommand
    {
        get => GetValue(SettingsCommandProperty);
        set => SetValue(SettingsCommandProperty, value);
    }

    public bool HasDetailsPane
    {
        get => GetValue(HasDetailsPaneProperty);
        set => SetValue(HasDetailsPaneProperty, value);
    }

    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!HasDetailsPane)
        {
            return;
        }

        _exitDelayToken?.Cancel(); // Cancel any pending collapse
        StatusBorder.Height = 300;
        StatusBorder.Background = Brushes.Transparent;
        BadgeContainer.Opacity = 0;
        ScoreContainer.Opacity = 0;
        TitleBorder.Height = double.NaN;
        TitleBorder.MaxHeight = 120;
        TitleTextBlock.FontWeight = FontWeight.Bold;
        TitleTextBlock.FontSize = 18;
        TitleTextBlock.TextWrapping = TextWrapping.WrapWithOverflow;
        TitleTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;
        if (ImageContainer.Effect is BlurEffect effect)
        {
            effect.Radius = 25;
        }

        Tint.IsVisible = true;
        Grid.SetRowSpan(ImageContainer, 2);
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

            if (!HasDetailsPane)
            {
                return;
            }

            StatusBorder.Height = 60;
            BadgeContainer.Opacity = 1;
            ScoreContainer.Opacity = 1;
            TitleBorder.Height = 54;
            TitleTextBlock.FontWeight = FontWeight.Normal;
            TitleTextBlock.FontSize = 15;
            TitleTextBlock.TextWrapping = TextWrapping.NoWrap;
            TitleTextBlock.TextTrimming = TextTrimming.CharacterEllipsis;
            if (ImageContainer.Effect is BlurEffect effect)
            {
                effect.Radius = 0;
            }

            Tint.IsVisible = false;
            Grid.SetRowSpan(ImageContainer, 1);
        }
        catch (Exception)
        {
            // Exit was canceled due to re-entry
        }
    }

    private void OnEditClicked(object? sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new NavigateToKeyDialogMessage
        {
            Title = Anime.Title,
            Key = $"tracking/{Anime.ServiceName}",
            Button = DialogButton.OKCancel,
            Data = Anime
        });
    }

    private void OnDownloadClicked(object? sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new NavigateToKeyDialogMessage
        {
            Title = Anime.Title,
            Data = Anime,
            Key = "Download",
            Button = DialogButton.OKCancel
        });
    }
}