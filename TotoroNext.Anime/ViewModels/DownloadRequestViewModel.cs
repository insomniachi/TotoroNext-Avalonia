using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class DownloadRequestViewModel(
    AnimeModel anime,
    IFactory<IAnimeProvider, Guid> providerFactory,
    IEnumerable<Descriptor> descriptors,
    IAnimeExtensionService extensionService,
    IAnimeDownloader animeDownloader,
    IDownloadManager downloadManager,
    ILocalSettingsService localSettingsService) : ObservableObject, IInitializable, IDialogViewModel
{
    private IAnimeProvider? _provider;

    [ObservableProperty] public partial Guid? ProviderId { get; set; }
    [ObservableProperty] public partial SearchResult? SelectedResult { get; set; }
    [ObservableProperty] public partial string? SearchTerm { get; set; }
    [ObservableProperty] public partial bool CanDownload { get; set; }
    [ObservableProperty] public partial int Start { get; set; }
    [ObservableProperty] public partial int End { get; set; }
    [ObservableProperty] public partial double TotalEpisodes { get; set; }
    [ObservableProperty] public partial int EpisodeOffset { get; set; }
    [ObservableProperty] public partial List<ModuleOptionItem> ProviderOptions { get; set; } = [];

    public List<Descriptor> Providers { get; } =
        [..descriptors.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];


    public async Task Handle(DialogResult result)
    {
        if (result != DialogResult.OK || SelectedResult is null || _provider is null)
        {
            return;
        }

        _provider.UpdateOptions(ProviderOptions);

        var request = new DownloadRequest
        {
            Anime = anime,
            Provider = _provider,
            SearchResult = SelectedResult,
            EpisodeStart = Start,
            EpisodeEnd = End,
            EpisodeOffset = EpisodeOffset
        };

        await foreach (var operation in animeDownloader.Download(request))
        {
            downloadManager.AddDownload(operation);
        }
    }

    public void Initialize()
    {
        var extensions = extensionService.GetExtension(anime.Id);
        ProviderId = extensions?.Provider ?? localSettingsService.ReadSetting<Guid>("SelectedAnimeProvider");
        End = anime.TotalEpisodes ?? 0;
        Start = (anime.Tracking?.WatchedEpisodes ?? 0) + 1;
        TotalEpisodes = anime.TotalEpisodes ?? double.NaN;

        this.WhenAnyValue(x => x.ProviderId)
            .WhereNotNull()
            .Where(x => x != Guid.Empty)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(id =>
            {
                SearchTerm = "";
                _provider = providerFactory.Create(id!.Value)!;
                ProviderOptions = _provider.GetOptions();
                extensionService.SearchOrSelectAsync(_provider, anime)
                                .ToObservable()
                                .Subscribe(searchResult =>
                                {
                                    if (searchResult is null)
                                    {
                                        return;
                                    }

                                    SelectedResult = searchResult;
                                });
            });

        this.WhenAnyValue(x => x.SelectedResult, x => x.Start, x => x.End)
            .Select(x => x.Item1 is not null &&
                         x is { Item2: > 0, Item3: > 0 } &&
                         x.Item3 >= x.Item2 &&
                         _provider is not null)
            .Subscribe(canDownload => CanDownload = canDownload);
    }

    public async Task<List<SearchResult>> GetSearchResults(string? term, CancellationToken ct)
    {
        if (_provider is null || string.IsNullOrEmpty(term))
        {
            return [];
        }

        try
        {
            return await _provider.SearchAsync(term, ct).ToListAsync(ct);
        }
        catch
        {
            return [];
        }
    }
}