using System.Runtime.CompilerServices;
using System.Text.Json;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen;

public class AnimeProvider(
    IModuleSettings<Settings> settings,
    IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();

        var response = await client.Request("search")
                                   .AppendPathSegment(query)
                                   .GetJsonAsync<ResultResponse<List<AnimeOnsenItemModel>>>(cancellationToken: ct);

        foreach (var item in response.Result ?? [])
        {
            ct.ThrowIfCancellationRequested();
            var image = $"https://api.animeonsen.xyz/v4/image/210x300/{item.Id}";
            yield return new SearchResult(this, item.Id, item.Title, new Uri(image));
        }
    }
    
    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var response = await client.Request($"content/{animeId}/episodes")
                                   .WithHeader(HeaderNames.Referer, Http.UserAgent)
                                   .GetJsonAsync<Dictionary<string, AnimeOnsenEpisode>>(cancellationToken: ct);

        foreach (var item in response)
        {
            ct.ThrowIfCancellationRequested();
            
            if (!float.TryParse(item.Key, out var number))
            {
                continue;
            }

            yield return new Episode(this, animeId, item.Key, number)
            {
                Info = new EpisodeInfo
                {
                    Titles = new Titles
                    {
                        English = item.Value.TitleEnglish,
                        Japanese = item.Value.TitleJapanese
                    }
                }
            };
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();

        var stream = await client.Request($"content/{animeId}/video/{episodeId}")
                                 .GetStreamAsync(cancellationToken: ct);

        var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var response = doc.RootElement.GetProperty("uri").Deserialize<AnimeOnsenStream>()!;
        var metadata = doc.RootElement.GetProperty("metadata");
        var episode = metadata.GetProperty("episode");
        var skipData = episode.EnumerateArray().ElementAt(1).Deserialize<AnimeOnsenSkipData>();

        yield return new VideoServer("Default", new Uri(response.Url))
        {
            Subtitle = response.Subtitles.Get(settings.Value.SubtitleLanguage),
            Headers =
            {
                [HeaderNames.Referer] = "https://www.animeonsen.xyz/"
            },
            SkipData = CovertSkipData(skipData)
        };
    }

    public List<ModuleOptionItem> GetOptions()
    {
        return settings.Value.ToModuleOptions();
    }

    public void UpdateOptions(List<ModuleOptionItem> options)
    {
        settings.Value.UpdateValues(options);
    }

    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient(typeof(Module).FullName!));
    }

    private static SkipData? CovertSkipData(AnimeOnsenSkipData? skipData)
    {
        if (skipData is null)
        {
            return null;
        }

        var data = new SkipData();
        if (skipData.OpeningEnd is not ("" or "0"))
        {
            data.Opening = new Segment
            {
                Start = TimeSpan.FromSeconds(int.Parse(skipData.OpeningStart)),
                End = TimeSpan.FromSeconds(int.Parse(skipData.OpeningEnd))
            };
        }

        if (skipData.EndingEnd is not ("" or "0"))
        {
            data.Opening = new Segment
            {
                Start = TimeSpan.FromSeconds(int.Parse(skipData.EndingStart)),
                End = TimeSpan.FromSeconds(int.Parse(skipData.EndingEnd))
            };
        }

        return data;
    }
}