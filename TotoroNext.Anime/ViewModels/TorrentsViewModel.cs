using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DynamicData;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class TorrentsViewModel : DialogViewModel, IInitializable
{
    private readonly AnimeModel _anime;
    private readonly ITorrentIndexer _indexer;
    private readonly IMessenger _messenger;
    private readonly ITorrentExtractor _torrentExtractor;
    private readonly ReadOnlyObservableCollection<Selectable<TorrentModel>> _torrents;
    private readonly SourceCache<Selectable<TorrentModel>, Uri> _torrentsCache = new(x => x.Value.Torrent);
    private bool _isAllSelected;

    public TorrentsViewModel(TorrentsViewModelNavigationParameters param,
                             IMessenger messenger,
                             ITorrentExtractor torrentExtractor,
                             IFactory<ITorrentIndexer, Guid> indexerFactory)
    {
        _messenger = messenger;
        _torrentExtractor = torrentExtractor;
        _indexer = indexerFactory.CreateDefault()!;
        _anime = param.Anime;
        Title = param.Anime.Title;

        _torrentsCache
            .Connect()
            .RefCount()
            .Bind(out _torrents)
            .DisposeMany()
            .Subscribe();
    }

    [ObservableProperty] public partial string ReleaseGroup { get; set; } = "SubsPlease";
    [ObservableProperty] public partial string Quality { get; set; } = "1080p";
    [ObservableProperty] public partial string Title { get; set; }

    public ReadOnlyObservableCollection<Selectable<TorrentModel>> Torrents => _torrents;
    public List<string> Qualities { get; } = ["720p", "1080p"];

    public List<string> ReleaseGroups { get; } =
    [
        "Anime Time",
        "ASW",
        "DKB",
        "EMBER",
        "Erai-raws",
        "Ironclad",
        "Judas",
        "New-raws",
        "Raze",
        "SubsPlease",
        "ToonsHub"
    ];

    public void Initialize()
    {
        this.WhenAnyValue(x => x.Title, x => x.ReleaseGroup, x => x.Quality)
            .Where(x => string.IsNullOrEmpty(x.Item2) || x.Item2.Length > 2)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .SelectMany(x => SearchTorrentsAsync(x.Item1, x.Item2, x.Item3))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(list =>
            {
                _torrentsCache.Clear();
                _torrentsCache.AddOrUpdate(list.Select(x => new Selectable<TorrentModel>(x)));
            });
    }

    private async Task<List<TorrentModel>> SearchTorrentsAsync(string title, string releaseGroup, string quality)
    {
        try
        {
            return await _indexer.SearchAsync(title, releaseGroup, quality).ToListAsync();
        }
        catch
        {
            return [];
        }
    }

    [RelayCommand]
    private void Download()
    {
        if (!Torrents.Any(x => x.IsSelected))
        {
            return;
        }

        Close();
    }

    [RelayCommand]
    private void Stream()
    {
        if (!Torrents.Any(x => x.IsSelected))
        {
            return;
        }

        var selected = Torrents.Where(x => x.IsSelected)
                               .Select(x => x.Value)
                               .OrderBy(x => x.Episode);
        var provider = new TorrentAnimeProvider(selected, _torrentExtractor);
        var result = new SearchResult(provider, _anime.Id.ToString(), _anime.Title);

        _messenger.Send(new NavigateToDataMessage(new WatchViewModelNavigationParameter(result, _anime)));

        Close();
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        foreach (var selectable in Torrents)
        {
            selectable.IsSelected = !_isAllSelected;
        }

        _isAllSelected = !_isAllSelected;
    }
}