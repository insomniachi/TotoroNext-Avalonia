using System.Reactive.Linq;
using Avalonia.Input;
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
    EpisodesListViewModelNavigationParameters @params,
    IFactory<IMetadataService, Guid> metadataFactory,
    IPlaybackProgressService playbackProgressService,
    IAnimeExtensionService animeExtensionService,
    IAnimeRelations relations,
    IMessenger messenger) : ObservableObject, IAsyncInitializable, ICloseable
{
    public AnimeModel Anime { get; } = @params.Anime;

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
        var searchResult = await animeExtensionService.SearchAndSelectAsync(Anime);

        if (searchResult is null)
        {
            return;
        }

        var episodes = await searchResult.GetEpisodes().ToListAsync();

        if (episodes.Count > (Anime.TotalEpisodes ?? 0) && relations.FindRelation(Anime) is { } relation)
        {
            episodes = episodes.Where(x => x.Number >= relation.SourceEpisodesRage.Start && x.Number <= relation.SourceEpisodesRage.End).ToList();
            foreach (var ep in episodes)
            {
                ep.Number -= relation.SourceEpisodesRage.Start - 1;
            }
        }
        
        var selectedEpisode = episodes.FirstOrDefault(x => (int)x.Number == episode.EpisodeNumber);

        if (selectedEpisode is null)
        {
            return;
        }

        if (episode.Progress is { } info)
        {
            selectedEpisode.StartPosition = TimeSpan.FromSeconds(info.Position);
        }
        
                
        Closed?.Invoke(this, EventArgs.Empty);

        messenger.Send(new NavigateToDataMessage(new WatchViewModelNavigationParameter(searchResult,
                                                                                       Anime,
                                                                                       episodes,
                                                                                       selectedEpisode,
                                                                                       false)));

    }

    private async Task UpdateEpisodes()
    {
        var metadataService = metadataFactory.CreateDefault();
        if (metadataService is null)
        {
            return;
        }
        
        IsLoading = true;
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

    public event EventHandler? Closed;
}