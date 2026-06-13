using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Media = TotoroNext.MediaEngine.Abstractions.Media;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class AnimeSongsViewModel(
    SongsViewModelNavigationParameters @params,
    IAnimeMusicService animeMusicService,
    IFactory<IMediaPlayer, Guid> mediaPlayerFactory,
    IMessenger messenger) : ObservableObject, IAsyncInitializable
{
    private TimeSpan _duration;
    private AnimeMusic? _selectedThemeObject;

    [ObservableProperty] public partial List<AnimeMusic> Themes { get; set; } = [];

    [ObservableProperty] public partial Uri? SelectedTheme { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial bool IsPlayingOrPaused { get; set; }

    public IEmbeddedVlcMediaPlayer EmbeddedVlcMediaPlayer { get; } = (IEmbeddedVlcMediaPlayer)mediaPlayerFactory.Create(Guid.Empty)!;

    public async Task InitializeAsync()
    {
        EmbeddedVlcMediaPlayer.StateChanged
                              .ObserveOn(RxSchedulers.MainThreadScheduler)
                              .Subscribe(state => IsPlayingOrPaused = state is MediaPlayerState.Playing or MediaPlayerState.Paused);

        SubscriptionsForRpc(EmbeddedVlcMediaPlayer);

        IsLoading = true;
        Themes = await animeMusicService.FindAll(@params.Anime);
        IsLoading = false;
    }

    [RelayCommand]
    private void Play(AnimeMusic music)
    {
        _selectedThemeObject = music;
        SelectedTheme = music.Video;
    }

    [RelayCommand]
    private void PlayAudio(AnimeMusic music)
    {
        if (music.Audio is not { } uri)
        {
            return;
        }

        _selectedThemeObject = music;
        var media = new Media(uri, new MediaMetadata(music.SongName));
        EmbeddedVlcMediaPlayer.Play(media, TimeSpan.Zero);
    }

    [RelayCommand]
    private void OpenInMediaPlayer(AnimeMusic music)
    {
        if (music.Video is not { } uri)
        {
            return;
        }

        _selectedThemeObject = music;
        var player = mediaPlayerFactory.CreateDefault();
        if (player is null)
        {
            return;
        }

        SubscriptionsForRpc(player);
        player.Play(new Media(uri, new MediaMetadata(music.DisplayName)), TimeSpan.Zero);
    }

    private void SubscriptionsForRpc(IMediaPlayer player)
    {
        player.PlaybackStopped
              .Subscribe(_ => messenger.Send(new PlaybackEnded { Id = SelectedTheme?.AbsolutePath ?? "" }));
        player.DurationChanged.Subscribe(ts => _duration = ts);
        player.PositionChanged.Subscribe(ts =>
        {
            if (_selectedThemeObject is null)
            {
                return;
            }

            messenger.Send(new SongPlaybackState
            {
                Anime = @params.Anime,
                Duration = _duration,
                Position = ts,
                Song = _selectedThemeObject
            });
        });
    }
}