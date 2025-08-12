using System.Text.Json;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen;

public class AnimeProvider(IModuleSettings<Settings> settings) : IAnimeProvider
{
    private readonly string _apiToken = settings.Value.ApiToken;

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        var token = await Settings.SearchTokenTaskCompletionSource.Task;

        var response = await "https://search.animeonsen.xyz/indexes/content/search"
                             .WithOAuthBearerToken(token)
                             .PostJsonAsync(new
                             {
                                 q = query
                             })
                             .ReceiveJson<AnimeOnsenSearchResult>();

        foreach (var item in response.Data)
        {
            var image = $"https://api.animeonsen.xyz/v4/image/210x300/{item.Id}";
            yield return new SearchResult(this, item.Id, item.Title, new Uri(image));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var stream = await $"https://api.animeonsen.xyz/v4/content/{animeId}/video/{episodeId}"
                           .WithOAuthBearerToken(_apiToken)
                           .GetStreamAsync();

        var doc = await JsonDocument.ParseAsync(stream);
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

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var response = await $"https://api.animeonsen.xyz/v4/content/{animeId}/episodes"
                             .WithOAuthBearerToken(_apiToken)
                             .GetJsonAsync<Dictionary<string, AnimeOnsenEpisode>>();

        foreach (var item in response)
        {
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