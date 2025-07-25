using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Media = TotoroNext.MediaEngine.Abstractions.Media;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class AnimeSongsViewModel(
    SongsViewModelNavigationParameters @params,
    IAnimeThemes animeThemes,
    IFactory<IMediaPlayer, Guid> mediaPlayerFactory) : ObservableObject, IAsyncInitializable
{
    [ObservableProperty] public partial List<Abstractions.AnimeTheme> Themes { get; set; } = [];

    [ObservableProperty] public partial Uri? SelectedTheme { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    [ObservableProperty] public partial bool IsPlayingOrPaused { get; set; }

    public IInternalMediaPlayer InternalMediaPlayer { get; } = (IInternalMediaPlayer)mediaPlayerFactory.Create(Guid.Empty);

    public async Task InitializeAsync()
    {
        InternalMediaPlayer.StateChanged
                           .ObserveOn(RxApp.MainThreadScheduler)
                           .Subscribe(state => IsPlayingOrPaused = state is MediaPlayerState.Playing or MediaPlayerState.Paused);

        IsLoading = true;

        Themes = await animeThemes.FindById(@params.Anime.Id, @params.Anime.ServiceName ?? nameof(ExternalIds.MyAnimeList));

        IsLoading = false;
    }

    [RelayCommand]
    private void Play(Abstractions.AnimeTheme theme)
    {
        SelectedTheme = theme.Video;
    }

    [RelayCommand]
    private void PlayAudio(Abstractions.AnimeTheme them)
    {
        if (them.Audio is not { } uri)
        {
            return;
        }

        var media = new Media(uri, new MediaMetadata(them.SongName));
        InternalMediaPlayer.Play(media, TimeSpan.Zero);
    }

    [RelayCommand]
    private void OpenInMediaPlayer(Abstractions.AnimeTheme theme)
    {
        if (theme.Video is not { } uri)
        {
            return;
        }

        var player = mediaPlayerFactory.CreateDefault();
        player.Play(new Media(uri, new MediaMetadata(theme.DisplayName)), TimeSpan.Zero);
    }
}