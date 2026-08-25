using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Local.Mapping;
using TotoroNext.Module;
using Ursa.Controls;

namespace TotoroNext.Anime.Local.ViewModels;

[UsedImplicitly]
internal partial class EditAnimeViewModel(
    AnimeModel anime,
    IDbContext dbContext,
    IHttpClientFactory httpClientFactory) : DialogViewModel
{
    private readonly OfflineAnimeModel _anime = dbContext.Anime.FindById(anime.Id);
    private readonly AnimeNewsNetwork _ann = new(httpClientFactory);
    private readonly Kitsu _kitsu = new(httpClientFactory);
    private readonly Anidb _anidb = new(httpClientFactory);
    
    [ObservableProperty] public partial AiringStatus Status { get; set; } = anime.AiringStatus;
    [ObservableProperty] public partial int TotalEpisodes { get; set; } = anime.TotalEpisodes ?? 0;
    [ObservableProperty] public partial long AniDbId { get; set; } = anime.ExternalIds.AniDb;
    [ObservableProperty] public partial long KitsuId { get; set; } = anime.ExternalIds.Kitsu;
    [ObservableProperty] public partial long AnnId { get; set; } = anime.ExternalIds.AnimeNewsNetwork;

    [RelayCommand]
    private async Task FetchAnnId()
    {
        AnnId = await _ann.TryGetId(_anime) ?? 0;
    }
    
    [RelayCommand]
    private async Task FetchAniDbId()
    {
        AniDbId = await _anidb.TryGetId(_anime) ?? 0;
    }
    
    [RelayCommand]
    private async Task FetchKitsuId()
    {
        KitsuId = await _kitsu.TryGetId(_anime) ?? 0;
    }

    public override Task Handle(DialogResult result)
    {
        if (result is not DialogResult.OK)
        {
            return Task.CompletedTask;
        }

        _anime.AiringStatus = Status;
        _anime.TotalEpisodes = TotalEpisodes;
        _anime.AniDbId = AniDbId;
        _anime.KitsuId = KitsuId;
        _anime.AnnId = AnnId;
        
        anime.AiringStatus = Status;
        anime.TotalEpisodes = TotalEpisodes;
        anime.ExternalIds.AniDb = AniDbId;
        anime.ExternalIds.Kitsu = KitsuId;
        anime.ExternalIds.AnimeNewsNetwork = AnnId;
        
        dbContext.Anime.Update(_anime);

        return Task.CompletedTask;
    }
}