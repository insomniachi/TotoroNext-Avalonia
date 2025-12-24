using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class HomeViewModel(IFactory<IMetadataService, Guid> metadataFactory) : ObservableObject, IAsyncInitializable, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly IMetadataService _metadataService = metadataFactory.CreateDefault()!;

    [ObservableProperty] public partial List<AnimeModel> HeroItems { get; set; } = [];

    [ObservableProperty] public partial Func<Task<List<AnimeModel>>>? PopulatePopular { get; set; }

    [ObservableProperty] public partial Func<Task<List<AnimeModel>>>? PopulateUpcoming { get; set; }

    [ObservableProperty] public partial Func<Task<List<AnimeModel>>>? PopulateAiringToday { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; } = true;

    public Task InitializeAsync()
    {
        PopulateAiringToday = async () =>
        {
            var airingToday = await _metadataService.GetAiringToday(_cts.Token);
            return airingToday.OrderBy(x => x.Tracking == null ? 1 : 0)
                              .ThenByDescending(x => x.MeanScore)
                              .ToList();
        };
        PopulatePopular = async () =>
        {
            var popular = await _metadataService.GetPopularAnimeAsync(_cts.Token);
            var current = AnimeHelpers.CurrentSeason();
            var items = popular.OrderByDescending(x => x.MeanScore)
                               .Where(x => x.Season == current)
                               .Take(5)
                               .ToList();

            if (_metadataService is not ILocalMetadataService localMetadataService)
            {
                HeroItems = items;
            }
            else
            {
                // local database doesn't have description
                // it's fetched and cached when requestion full data.
                HeroItems = await PopulateData(items, localMetadataService).ToListAsync();
            }
            
            IsLoading = false;
            return popular;
        };
        PopulateUpcoming = () => _metadataService.GetUpcomingAnimeAsync(_cts.Token);


        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private static async IAsyncEnumerable<AnimeModel> PopulateData(IEnumerable<AnimeModel> partial, ILocalMetadataService metadataService)
    {
        foreach (var anime in partial)
        {
            yield return await metadataService.GetAnimeAsync(anime.Id);
        }
    }
}