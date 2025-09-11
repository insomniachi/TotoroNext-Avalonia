using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ReactiveUI;

namespace TotoroNext.MediaEngine.Abstractions.Controls;

public partial class TransportControlsViewModel(IEmbeddedVlcMediaPlayer mediaPlayer) : ObservableObject
{
    [ObservableProperty] public partial TimeSpan Duration { get; set; }
    [ObservableProperty] public partial long CurrentPositionTicks { get; set; }
    [ObservableProperty] public partial MediaPlayerState CurrentState { get; set; }
    [ObservableProperty] public partial bool IsSeeking { get; set; }
    [ObservableProperty] public partial TimeSpan ActualPosition { get; set; }
    [ObservableProperty] public partial TimeSpan TimeRemaining { get; set; }
    [ObservableProperty] public partial Thickness PlayPauseButtonMargin { get; set; } = new(6, 0, 0, 0);


    public void Initialize()
    {
        mediaPlayer.PositionChanged
                   .Do(ts =>
                   {
                       ActualPosition = ts;
                       TimeRemaining = Duration - ts;
                   })
                   .Where(_ => !IsSeeking)
                   .Subscribe(ts => CurrentPositionTicks = ts.Ticks);

        mediaPlayer.DurationChanged.Subscribe(ts =>
        {
            Duration = ts;
            TimeRemaining = ts;
        });

        mediaPlayer.StateChanged
                   .ObserveOn(RxApp.MainThreadScheduler)
                   .Do(state =>
                   {
                       switch (state)
                       {
                           case MediaPlayerState.Playing:
                               PlayPauseButtonMargin = new Thickness(0);
                               break;
                           case MediaPlayerState.Paused:
                               PlayPauseButtonMargin = new Thickness(6, 0, 0, 0);
                               break;
                       }
                   })
                   .Subscribe(state => CurrentState = state);
    }

    public void CompleteSeek()
    {
        mediaPlayer.SeekTo(TimeSpan.FromTicks(CurrentPositionTicks));
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        switch (mediaPlayer.CurrentState)
        {
            case MediaPlayerState.Playing:
                mediaPlayer.Pause();
                break;
            case MediaPlayerState.Paused:
                mediaPlayer.MediaPlayer.Play();
                PlayPauseButtonMargin = new Thickness(0);
                break;
            case MediaPlayerState.Opening:
            case MediaPlayerState.Stopped:
            case MediaPlayerState.Ended:
            case MediaPlayerState.Error:
            default:
                break;
        }
    }

    [RelayCommand]
    private void SeekForward()
    {
        mediaPlayer.SeekTo(TimeSpan.FromTicks(CurrentPositionTicks) + TimeSpan.FromSeconds(30));
    }

    [RelayCommand]
    private void SeekBackward()
    {
        mediaPlayer.SeekTo(TimeSpan.FromTicks(CurrentPositionTicks) - TimeSpan.FromSeconds(30));
    }

    [RelayCommand]
    private static void ToggleFullscreen()
    {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var mainWindow = lifetime?.MainWindow;

        if (mainWindow is null)
        {
            return;
        }

        if (mainWindow.WindowState == WindowState.FullScreen)
        {
            WeakReferenceMessenger.Default.Send<ExitFullScreen>();
        }
        else
        {
            WeakReferenceMessenger.Default.Send<EnterFullScreen>();
        }
    }
}