using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public sealed partial class AnimeDetailsViewModel(
    AnimeModel anime,
    IFactory<IMetadataService, Guid> metaFactory,
    IFactory<ITrackingService, Guid> trackerFactory) : ObservableObject, IAsyncInitializable, INavigatorHost
{
    private readonly IMetadataService _metadataService = metaFactory.CreateDefault();

    [ObservableProperty] public partial AnimeModel Anime { get; set; } = anime;

    [ObservableProperty] public partial ListItemStatus? Status { get; set; } = anime.Tracking?.Status;

    [ObservableProperty] public partial int Progress { get; set; } = anime.Tracking?.WatchedEpisodes ?? 0;

    [ObservableProperty] public partial int Score { get; set; } = anime.Tracking?.Score ?? 0;

    [ObservableProperty] public partial DateTime? StartDate { get; set; } = anime.Tracking?.StartDate;

    [ObservableProperty] public partial DateTime? FinishDate { get; set; } = anime.Tracking?.FinishDate;

    [ObservableProperty] public partial AnimeDetailsTabItem? SelectedTab { get; set; }

    public ListItemStatus[] Statuses { get; } = [.. Enum.GetValues<ListItemStatus>()];

    public ObservableCollection<AnimeDetailsTabItem> Tabs { get; } =
    [
        new("Episodes", anime => new EpisodesListViewModelNagivationParameters(anime)),
        new("Related", anime => anime.Related.ToList()),
        new("Recommended", anime => anime.Recommended.ToList()),
        new("Overrides", anime => new OverridesViewModelNavigationParameters(anime)),
        new("Songs", anime => new SongsViewModelNavigationParameters(anime))
    ];

    public async Task InitializeAsync()
    {
        Anime = await _metadataService.GetAnimeAsync(Anime.Id) ?? Anime;
        SelectedTab = Tabs.First();

        this.WhenAnyValue(x => x.Status, x => x.Progress, x => x.Score, x => x.StartDate, x => x.FinishDate)
            .Skip(1)
            .Select(x => new Tracking
            {
                Status = x.Item1,
                WatchedEpisodes = x.Item2,
                Score = x.Item3,
                StartDate = x.Item4,
                FinishDate = x.Item5
            })
            .SelectMany(tracking => UpdateTracking(Anime, tracking).ToObservable())
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedTab)
            .WhereNotNull()
            .Subscribe(tab => Navigator?.NavigateToData(tab.GetData(Anime)));
    }

    [ObservableProperty] public partial INavigator? Navigator { get; set; }

    private async Task UpdateTracking(AnimeModel anime, Tracking tracking)
    {
        var trackingServices = trackerFactory.CreateAll();
        foreach (var trackingService in trackingServices)
        {
            var id = anime.ExternalIds.GetId(trackingService.Name);
            if (id is null)
            {
                try
                {
                    var metaDataService = metaFactory.Create(trackingService.Id);
                    if (metaDataService.Id == anime.ServiceId)
                    {
                        continue;
                    }

                    id = (await metaDataService.FindAnimeAsync(anime))?.Id;
                }
                catch
                {
                    continue;
                }
            }

            if (id is null)
            {
                continue;
            }

            await trackingService.Update(id.Value, tracking);
        }
    }
}

public class AnimeDetailsTabItem(string title, Func<AnimeModel, object> getData)
{
    public string Title { get; } = title;
    public Func<AnimeModel, object> GetData { get; } = getData;
}