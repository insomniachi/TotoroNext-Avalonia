using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class AnimeEpisodesListViewModel(
    EpisodesListViewModelNagivationParameters @params,
    IFactory<IMetadataService, Guid> metadataFactory,
    IPlaybackProgressService playbackProgressService,
    IFactory<IAnimeProvider, Guid> providerFactory,
    IMessenger messenger) : ObservableObject, IAsyncInitializable
{
    [ObservableProperty] public partial AnimeModel Anime { get; set; } = @params.Anime;

    [ObservableProperty] public partial List<EpisodeInfo> Episodes { get; set; } = [];

    [ObservableProperty] public partial EpisodeInfo? SelectedEpisode { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }

    public async Task InitializeAsync()
    {
        await UpdateEpisodes();

        this.WhenAnyValue(x => x.Episodes)
            .Where(x => x is { Count: > 0 })
            .Subscribe(_ => SelectedEpisode = GetNextUp());
    }

    [RelayCommand]
    private async Task WatchEpisode(EpisodeInfo episode)
    {
        var provider = providerFactory.CreateDefault();
        var searchResult = await provider.SearchAndSelectAsync(Anime);

        if (searchResult is null)
        {
            return;
        }

        var episodes = await searchResult.GetEpisodes().ToListAsync();
        var selectedEpisode = episodes.FirstOrDefault(x => (int)x.Number == episode.EpisodeNumber);

        if (selectedEpisode is null)
        {
            return;
        }

        if (episode.Progress is { } info)
        {
            selectedEpisode.StartPosition = TimeSpan.FromSeconds(info.Position);
        }

        messenger.Send(new NavigateToDataMessage(new WatchViewModelNavigationParameter(searchResult,
                                                                                       Anime,
                                                                                       episodes,
                                                                                       selectedEpisode,
                                                                                       false)));
    }

    private async Task UpdateEpisodes()
    {
        IsLoading = true;

        var metadataService = metadataFactory.CreateDefault();
        var eps = await metadataService.GetEpisodesAsync(Anime);
        var progress = playbackProgressService.GetProgress(Anime.Id);

        foreach (var item in progress)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (eps.FirstOrDefault(x => x.EpisodeNumber == item.Key) is { } ep)
            {
                ep.Progress = item.Value;
            }
        }

        Episodes = eps;

        IsLoading = false;
    }

    private EpisodeInfo? GetNextUp()
    {
        return Anime is { Tracking.WatchedEpisodes: 0 or null }
            ? Episodes.FirstOrDefault()
            : Episodes.FirstOrDefault(x => x.EpisodeNumber == (Anime.Tracking?.WatchedEpisodes ?? 0) + 1);
    }
}