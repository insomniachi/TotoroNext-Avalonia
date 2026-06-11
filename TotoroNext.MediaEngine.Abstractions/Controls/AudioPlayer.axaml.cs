using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;
using TotoroNext.Module;

namespace TotoroNext.MediaEngine.Abstractions.Controls;

public partial class AudioPlayer : UserControl
{
    public static readonly StyledProperty<IEmbeddedVlcMediaPlayer?> MediaPlayerProperty =
        AvaloniaProperty.Register<AudioPlayer, IEmbeddedVlcMediaPlayer?>("MediaPlayer");

    public AudioPlayer()
    {
        InitializeComponent();

        MediaPlayerProperty.Changed.AddClassHandler<AudioPlayer>(OnMediaPlayerChanged);
    }

    public IEmbeddedVlcMediaPlayer? MediaPlayer
    {
        get => GetValue(MediaPlayerProperty);
        set => SetValue(MediaPlayerProperty, value);
    }

    private static void OnMediaPlayerChanged(AudioPlayer player, AvaloniaPropertyChangedEventArgs arg)
    {
        if (arg.NewValue is not IEmbeddedVlcMediaPlayer { } mediaPlayer)
        {
            return;
        }

        if (player.Find<Slider>("PositionSlider") is not { } slider ||
            player.Find<Viewbox>("IconControl") is not { } icon)
        {
            return;
        }

        mediaPlayer.StateChanged
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(state =>
                   {
                       icon.Child = state switch
                       {
                           MediaPlayerState.Playing => IconRegistry.GetPathIcon(CommonIcons.Pause),
                           MediaPlayerState.Paused => IconRegistry.GetPathIcon(CommonIcons.Play),
                           _ => icon.Child
                       };
                   });
        mediaPlayer.DurationChanged
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(value => slider.Maximum = value.TotalSeconds);
        mediaPlayer.PositionChanged
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Subscribe(value => slider.Value = value.TotalSeconds);
    }

    private void PlayPauseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (MediaPlayer is null)
        {
            return;
        }

        var icon = this.Find<Viewbox>("IconControl");
        if (icon is null)
        {
            return;
        }

        switch (MediaPlayer.CurrentState)
        {
            case MediaPlayerState.Playing:
                MediaPlayer.Pause();
                break;
            case MediaPlayerState.Paused:
                MediaPlayer.Play();
                break;
            case MediaPlayerState.Opening:
            case MediaPlayerState.Stopped:
            case MediaPlayerState.Ended:
            case MediaPlayerState.Error:
            default:
                break;
        }
    }
}