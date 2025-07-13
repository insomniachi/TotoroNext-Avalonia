using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class AnimeSongsViewModel(
    SongsViewModelNavigationParameters @params,
    IAnimeThemes animeThemes,
    IHttpClientFactory httpClientFactory,
    IFactory<IMediaPlayer, Guid> mediaPlayerFactory) : ObservableObject, IAsyncInitializable
{
    [ObservableProperty] public partial List<Abstractions.AnimeTheme> Themes { get; set; } = [];

    [ObservableProperty] public partial Uri? SelectedTheme { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    public async Task InitializeAsync()
    {
        IsLoading = true;

        Themes = await animeThemes.FindById(@params.Anime.Id, @params.Anime.ServiceType ?? "MyAnimeList");

        IsLoading = false;
    }

    [RelayCommand]
    private void Play(Abstractions.AnimeTheme theme)
    {
        SelectedTheme = theme.Video;
    }

    [RelayCommand]
    private void PlayAudio(Abstractions.AnimeTheme them) { }

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