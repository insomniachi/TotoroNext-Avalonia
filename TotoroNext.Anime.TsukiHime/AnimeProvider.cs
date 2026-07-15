using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AnitomySharp;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TsukiHime;

public class AnimeProvider(
    IHttpClientFactory httpClientFactory,
    IModuleSettings<Settings> settings) : IAnimeProvider
{
    public const string BaseUrl = "https://api.tsukihime.org/v1/";
    private readonly IVideoExtractor _extractor = new TsukiHimeExtractor();

    public async IAsyncEnumerable<SearchResult> SearchAsync(SearchOptions options, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request("animes", "mal", options.Ids?.MyAnimeList)
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var id = doc.RootElement.GetProperty("id").GetInt32();
        var title = doc.RootElement.GetProperty("title").GetString()!;
        var image = doc.RootElement.GetProperty("thumbnail").GetString()!;

        yield return new SearchResult(this, id.ToString(), title, new Uri(image));
    }

    public IAsyncEnumerable<SearchResult> SearchAsync(string query, CancellationToken ct)
    {
        return TsukiHimeLocalData.Search(query)
                                 .Select(x => new SearchResult(this, x.Id.ToString(), x.Title))
                                 .ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var stream = await client.Request("animes", animeId, "episodes")
                                 .GetStreamAsync(cancellationToken: ct);
        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

        foreach (var id in doc.RootElement.EnumerateArray().Select(episode => episode.GetProperty("episode_num").GetInt32()))
        {
            yield return new Episode(this, animeId, id.ToString(), id);
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.Request("animes", animeId, "episodes", episodeId)
                                   .GetJsonAsync<EpisodeTorrentsListResponse>(cancellationToken: ct);

        var descriptor = TsukiHimeLocalData.Groups.FirstOrDefault(x => x.Name == settings.Value.Group);
        
        foreach (var torrent in response.Results.Where(x => x.Group.Id == descriptor?.Id))
        {
            var name = TorrentName.Parse(torrent);
            yield return new VideoServer(name.GetDisplayName(), new Uri($"https://api.tsukihime.org/v1/torrents/{torrent.Id}"), _extractor)
            {
                ContentType = "mkv",
                DownloaderType = DownloaderTypes.Http,
                IsDefault = name.Resolution?.Contains(settings.Value.Resolution) == true
            };
        }
    }
    
    public List<ModuleOptionItem> GetOptions() => settings.Value.ToModuleOptions();
    
    public void UpdateOptions(List<ModuleOptionItem> options) => settings.Value.UpdateValues(options);

    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient($"{Module.Id}-api"));
    }

    class TorrentName
    {
        public string? Resolution { get; init; }
        public required string Group { get; init; }
        public string? Video { get; init; }
        public string? Audio { get; init; }
        public string? Source { get; init; }

        public static TorrentName Parse(TorrentDescriptor descriptor)
        {
            var parts = AnitomySharp.AnitomySharp.Parse(descriptor.Name).ToList();
            var resolution = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementVideoResolution)?.Value;
            var encodingVideo = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementVideoTerm)?.Value;
            var encodingAudio = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementAudioTerm)?.Value;
            var source = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementSource)?.Value;
            return new TorrentName()
            {
                Group = descriptor.Group.Name,
                Resolution = resolution,
                Video = encodingVideo,
                Audio = encodingAudio,
                Source = source
            };
        }
        
        public string GetDisplayName()
        {
            var sb = new StringBuilder();
            sb.Append($"[{Group}]");
            if (!string.IsNullOrEmpty(Resolution))
            {
                sb.Append($" [{Resolution}]");
            }

            if (!string.IsNullOrEmpty(Source))
            {
                sb.Append($" [{Source}]");
            }

            if (!string.IsNullOrEmpty(Video))
            {
                sb.Append($" [{Video}]");
            }

            if (!string.IsNullOrEmpty(Audio))
            {
                sb.Append($" [{Audio}]");
            }

            return sb.ToString();
        }
    }
}