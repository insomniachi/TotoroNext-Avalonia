using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Media = TotoroNext.MediaEngine.Abstractions.Media;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public partial class ProviderDebuggerViewModel(
    IEnumerable<Descriptor> descriptors,
    IFactory<IAnimeProvider, Guid> providerFactory,
    IFactory<IMediaPlayer, Guid> playerFactory,
    SettingsModel settings) : ObservableObject, IInitializable
{
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

    [ObservableProperty] public partial VideoServer? SelectedServer { get; set; }

    public void Initialize()
    {
        List<Descriptor> all = [..descriptors];
        AnimeProviders = all.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider)).OrderBy(x => x.Name).ToList();
        MediaPlayers = all.Where(x => x.Components.Contains(ComponentTypes.MediaEngine)).ToList();

        ProviderId = settings.SelectedAnimeProvider;
        MediaPlayerId = settings.SelectedMediaEngine;

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
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(response =>
            {
                Episodes = [];
                Servers = [];
                Result = response;
            });

        this.WhenAnyValue(x => x.SelectedResult)
            .WhereNotNull()
            .SelectMany(AnimeProviderExtensions.GetEpisodes)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ep =>
            {
                Servers = [];
                Episodes = ep;
            });

        this.WhenAnyValue(x => x.SelectedEpisode)
            .WhereNotNull()
            .Do(_ => Servers = [])
            .SelectMany(AnimeProviderExtensions.GetServers)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(servers => Servers = servers);

        this.WhenAnyValue(x => x.SelectedServer)
            .WhereNotNull()
            .SelectMany(AnimeProviderExtensions.GetSources)
            .Select(x => x.FirstOrDefault())
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(Play);
    }

    private void Play(VideoSource source)
    {
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
        player.Play(media, SelectedEpisode.StartPosition);
    }
}