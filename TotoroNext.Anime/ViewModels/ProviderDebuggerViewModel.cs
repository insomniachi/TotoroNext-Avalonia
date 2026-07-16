using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Media = TotoroNext.MediaEngine.Abstractions.Media;

namespace TotoroNext.Anime.ViewModels;

[UsedImplicitly]
public partial class ProviderDebuggerViewModel(
    IEnumerable<Descriptor> descriptors,
    IFactory<IAnimeProvider, Guid> providerFactory,
    IFactory<IMediaPlayer, Guid> playerFactory,
    IFactory<IMetadataService, Guid> metadataFactory,
    IMessenger messenger,
    IDialogService dialogService,
    IAnimeDownloader downloader,
    IDownloadManager downloadManager,
    IClipboard clipboard,
    ILocalSettingsService localSettingsService) : ObservableObject, IInitializable
{
    private readonly IMetadataService? _metadataService = metadataFactory.CreateDefault();
    private AnimeModel? _anime;
    private IAnimeProvider? _provider;

    [ObservableProperty] public partial List<Descriptor> AnimeProviders { get; set; } = [];

    [ObservableProperty] public partial List<Descriptor> MediaPlayers { get; set; } = [];

    [ObservableProperty] public partial Guid? ProviderId { get; set; }

    [ObservableProperty] public partial Guid? MediaPlayerId { get; set; }

    [ObservableProperty] public partial string Query { get; set; } = "";

    [ObservableProperty] public partial List<SearchResult> Result { get; set; } = [];

    [ObservableProperty] public partial SearchResult? SelectedResult { get; set; }

    [ObservableProperty] public partial List<Episode> Episodes { get; set; } = [];

    [ObservableProperty] public partial Episode? SelectedEpisode { get; set; }

    [ObservableProperty] public partial List<VideoServer> Servers { get; set; } = [];

    public void Initialize()
    {
        List<Descriptor> all = [..descriptors];
        AnimeProviders = all.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider)).OrderBy(x => x.Name).ToList();
        MediaPlayers = all.Where(x => x.Components.Contains(ComponentTypes.MediaEngine)).ToList();

        ProviderId = localSettingsService.ReadSetting<Guid>("SelectedAnimeProvider");
        MediaPlayerId = localSettingsService.ReadSetting<Guid>("SelectedMediaEngine");

        this.WhenAnyValue(x => x.ProviderId)
            .WhereNotNull()
            .Subscribe(id =>
            {
                Result = [];
                Episodes = [];
                Servers = [];
                _provider = providerFactory.Create(id!.Value);
            });

        this.WhenAnyValue(x => x.Query)
            .Where(x => x is { Length: > 2 })
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Select(term => Observable.FromAsync(ct => _provider.GetSearchResults(term, ct)))
            .Switch()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(response =>
            {
                Episodes = [];
                Servers = [];
                Result = response;
            });

        this.WhenAnyValue(x => x.SelectedResult)
            .WhereNotNull()
            .Select(result => Observable.FromAsync(result.GetEpisodes))
            .Switch()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(ep =>
            {
                Servers = [];
                Episodes = ep;
            });

        this.WhenAnyValue(x => x.SelectedEpisode)
            .WhereNotNull()
            .Do(_ => Servers = [])
            .Select(ep => Observable.FromAsync(ep.GetServers))
            .Switch()
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(servers => Servers = servers);
    }

    [RelayCommand]
    private async Task Play(VideoServer server)
    {
        var source = (await server.Extract(CancellationToken.None).ToListAsync()).First();

        if (SelectedEpisode is null || MediaPlayerId is null)
        {
            return;
        }

        IEnumerable<string?> parts =
            [SelectedResult?.Title, $"Episode {SelectedEpisode.Number}", source.Title ?? SelectedEpisode.Info?.Titles.English];
        var title = string.Join(" - ", parts.Where(x => !string.IsNullOrEmpty(x)));

        var metadata = new MediaMetadata(title, source.Headers, Subtitle: source.Subtitle);
        var media = new Media(source.Url, metadata);
        var player = playerFactory.Create(MediaPlayerId.Value);
        if (player is null)
        {
            return;
        }

        var context = new TrackingMediaPlayerContext(player, messenger);
        if (_anime is not null)
        {
            context.Anime = _anime;
            context.SelectedEpisode = SelectedEpisode;
            context.Initialize();
        }

        context.Play(media, SelectedEpisode.StartPosition);
    }

    partial void OnSelectedResultChanged(SearchResult? value)
    {
        if (value is null || _metadataService is null)
        {
            return;
        }

        _metadataService.SearchAndSelectAsync(value)
                        .ToObservable()
                        .Subscribe(anime => _anime = anime);
    }

    [RelayCommand]
    private async Task Download(VideoServer server)
    {
        if (_anime is null || SelectedEpisode is null)
        {
            return;
        }

        var download = await downloader.Download(_anime, SelectedEpisode, server);

        if (download is null)
        {
            return;
        }

        downloadManager.AddDownload(download);
    }

    [RelayCommand]
    private async Task CopyToClipboard(VideoServer server)
    {
        var source = (await server.Extract(CancellationToken.None).ToListAsync()).First();
        var sb = new StringBuilder();
        sb.Append("curl ").Append($"'{source.Url}'").AppendLine(@" \");
        foreach (var kvp in source.Headers)
        {
            sb.Append("-H '").Append(kvp.Key).Append(": ").Append(kvp.Value).Append(@"' \");
        }

        await clipboard.SetTextAsync(sb.ToString());
    }

    [RelayCommand]
    private async Task EditOptions()
    {
        if (_provider?.GetOptions() is not { Count: > 0 } options)
        {
            return;
        }

        var title = AnimeProviders.FirstOrDefault(x => x.Id == ProviderId)?.Name ?? "";

        var isUpdated = await dialogService.EditModuleOptions(title, options);

        if (!isUpdated)
        {
            return;
        }

        _provider.UpdateOptions(options);
        Result = [];
        Episodes = [];
        Servers = [];
    }
}