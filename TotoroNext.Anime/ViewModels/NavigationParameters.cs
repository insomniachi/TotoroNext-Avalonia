using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.ViewModels;

public record EpisodesListViewModelNavigationParameters(AnimeModel Anime);

public record OverridesViewModelNavigationParameters(AnimeModel Anime);

public record SongsViewModelNavigationParameters(AnimeModel Anime);

public record WatchViewModelNavigationParameter(
    SearchResult ProviderResult,
    AnimeModel? Anime = null,
    List<Episode>? Episodes = null,
    Episode? SelectedEpisode = null,
    bool ContinueWatching = true);

public record InfoViewNavigationParameters(AnimeModel Anime);
