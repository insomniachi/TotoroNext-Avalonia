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
    IAnimeThemes animeThemes,
    IFactory<IMediaPlayer, Guid> mediaPlayerFactory,
    IMessenger messenger) : ObservableObject, IAsyncInitializable
{
    private TimeSpan _duration;
    private Abstractions.AnimeTheme? _selectedThemeObject;

    [ObservableProperty] public partial List<Abstractions.AnimeTheme> Themes { get; set; } = [];

    [ObservableProperty] public partial Uri? SelectedTheme { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial bool IsPlayingOrPaused { get; set; }

    public IEmbeddedVlcMediaPlayer EmbeddedVlcMediaPlayer { get; } = (IEmbeddedVlcMediaPlayer)mediaPlayerFactory.Create(Guid.Empty);

    public async Task InitializeAsync()
    {
        EmbeddedVlcMediaPlayer.StateChanged
                              .ObserveOn(RxApp.MainThreadScheduler)
                              .Subscribe(state => IsPlayingOrPaused = state is MediaPlayerState.Playing or MediaPlayerState.Paused);

        SubscriptionsForRpc(EmbeddedVlcMediaPlayer);

        IsLoading = true;

        Themes = await animeThemes.FindById(@params.Anime.Id, @params.Anime.ServiceName ?? nameof(AnimeId.MyAnimeList));

        IsLoading = false;
    }

    [RelayCommand]
    private void Play(Abstractions.AnimeTheme theme)
    {
        _selectedThemeObject = theme;
        SelectedTheme = theme.Video;
    }

    [RelayCommand]
    private void PlayAudio(Abstractions.AnimeTheme theme)
    {
        if (theme.Audio is not { } uri)
        {
            return;
        }

        _selectedThemeObject = theme;
        var media = new Media(uri, new MediaMetadata(theme.SongName));
        EmbeddedVlcMediaPlayer.Play(media, TimeSpan.Zero);
    }

    [RelayCommand]
    private void OpenInMediaPlayer(Abstractions.AnimeTheme theme)
    {
        if (theme.Video is not { } uri)
        {
            return;
        }

        _selectedThemeObject = theme;
        var player = mediaPlayerFactory.CreateDefault();
        if (player is null)
        {
            return;
        }

        SubscriptionsForRpc(player);
        player.Play(new Media(uri, new MediaMetadata(theme.DisplayName)), TimeSpan.Zero);
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