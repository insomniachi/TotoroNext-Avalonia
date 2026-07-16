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
using TotoroNext.Torrents.Abstractions.Models;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class TorrentsViewModel : DialogViewModel, IInitializable
{
    private readonly AnimeModel _anime;
    private readonly ITorrentIndexer _indexer;
    private readonly IMessenger _messenger;
    private readonly ITorrentClient _torrentClient;
    private readonly ITorrentExtractor _torrentExtractor;
    private readonly ReadOnlyObservableCollection<Selectable<AnimeTorrentModel>> _torrents;
    private readonly SourceCache<Selectable<AnimeTorrentModel>, Uri> _torrentsCache = new(x => x.Value.Torrent);
    private bool _isAllSelected;

    public TorrentsViewModel(TorrentsViewModelNavigationParameters param,
                             IMessenger messenger,
                             ITorrentExtractor torrentExtractor,
                             IFactory<ITorrentIndexer, Guid> indexerFactory,
                             IFactory<ITorrentClient, Guid> torrentClientFactory)
    {
        _messenger = messenger;
        _torrentExtractor = torrentExtractor;
        _indexer = indexerFactory.CreateDefault()!;
        _torrentClient = torrentClientFactory.CreateDefault()!;
        _anime = param.Anime;
        Title = param.Anime.RomajiTitle;
        ReleaseGroups = _indexer.GetReleaseGroups().ToList();

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

    public ReadOnlyObservableCollection<Selectable<AnimeTorrentModel>> Torrents => _torrents;
    public List<string> Qualities { get; } = ["480", "720p", "1080p"];

    public List<string> ReleaseGroups { get; }

    public void Initialize()
    {
        this.WhenAnyValue(x => x.Title, x => x.ReleaseGroup, x => x.Quality)
            .Where(x => string.IsNullOrEmpty(x.Item2) || x.Item2.Length > 2)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(x => new TorrentSearchOptions()
            {
                Query = x.Item1,
                GroupName = x.Item2,
                Quality = x.Item3,
                MyAnimeListId = _anime.ExternalIds.MyAnimeList
            })
            .SelectMany(SearchTorrentsAsync)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(list =>
            {
                _torrentsCache.Clear();
                _torrentsCache.AddOrUpdate(list.Select(x => new Selectable<AnimeTorrentModel>(x)));
            });
    }

    private async Task<List<AnimeTorrentModel>> SearchTorrentsAsync(TorrentSearchOptions options)
    {
        try
        {
            return await _indexer.SearchAsync(options).ToListAsync();
        }
        catch
        {
            return [];
        }
    }

    [RelayCommand]
    private async Task Download()
    {
        if (!Torrents.Any(x => x.IsSelected))
        {
            return;
        }

        var sanitizedTitle = RemoveIllegalPathCharacters(_anime.Title);
        var dir = FileHelper.GetPath(Path.Combine("Downloads", sanitizedTitle));
        await _torrentClient.AddTorrent(new AddTorrentRequest
        {
            Torrents = [.. Torrents.Where(x => x.IsSelected).Select(x => x.Value.Torrent.ToString())],
            SaveDirectory = dir,
        });

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

    private static string RemoveIllegalPathCharacters(string path)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(path.Where(c => !invalidChars.Contains(c)).ToArray());
    }
}