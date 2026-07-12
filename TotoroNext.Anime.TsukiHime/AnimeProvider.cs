using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AnitomySharp;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
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
        return AsyncEnumerable.Empty<SearchResult>();
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

        foreach (var torrent in response.Results.Where(x => x.Group.Id == settings.Value.Group))
        {
            yield return new VideoServer(ParseStreamName(torrent), new Uri($"https://api.tsukihime.org/v1/torrents/{torrent.Id}"), _extractor)
            {
                ContentType = "mkv",
                DownloaderType = DownloaderTypes.Http
            };
        }
    }

    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient($"{Module.Id}-api"));
    }

    private static string ParseStreamName(TorrentDescriptor torrent)
    {
        var parts = AnitomySharp.AnitomySharp.Parse(torrent.Name).ToList();
        var resolution = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementVideoResolution)?.Value ?? "";
        var encodingVideo = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementVideoTerm)?.Value ?? "";
        var encodingAudio = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementAudioTerm)?.Value ?? "";
        var source = parts.FirstOrDefault(x => x.Category == Element.ElementCategory.ElementSource)?.Value ?? "";
        var sb = new StringBuilder();
        sb.Append($"[{torrent.Group.Name}]");
        if (!string.IsNullOrEmpty(resolution))
        {
            sb.Append($" [{resolution}]");
        }

        if (!string.IsNullOrEmpty(source))
        {
            sb.Append($" [{source}]");
        }

        if (!string.IsNullOrEmpty(encodingVideo))
        {
            sb.Append($" [{encodingVideo}]");
        }

        if (!string.IsNullOrEmpty(encodingAudio))
        {
            sb.Append($" [{encodingAudio}]");
        }

        return sb.ToString();
    }
}