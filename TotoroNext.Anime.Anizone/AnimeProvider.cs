using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Anizone;

public class AnimeProvider : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        var stream = await "https://anizone.to/anime"
                           .AppendQueryParam("search", query)
                           .GetStreamAsync();

        var doc = new HtmlDocument();
        doc.Load(stream);

        foreach (var div in doc.QuerySelectorAll("div.grid > div") ?? [])
        {
            var id = div.GetAttributeValue("wire:key", "")["a-".Length..];
            var title = div.QuerySelector("a.text-white").GetAttributeValue("title", "");
            var image = div.QuerySelector("img").GetAttributeValue("src", "");
            yield return new SearchResult(this, id, title, new Uri(image));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var detailsUrl = $"https://anizone.to/anime/{animeId}/{episodeId}";
        var stream = await detailsUrl.GetStreamAsync();

        var doc = new HtmlDocument();
        doc.Load(stream);

        var mediaPlayer = doc.QuerySelector("media-player");
        var src = mediaPlayer.GetAttributeValue("src", "");
        var subtitle = "";
        foreach (var track in mediaPlayer.QuerySelectorAll("track"))
        {
            var lang = track.GetAttributeValue("srclang", "");
            if (lang != "en")
            {
                continue;
            }

            subtitle = track.GetAttributeValue("src", "");
            break;
        }

        yield return new VideoServer("Default", new Uri(src))
        {
            Subtitle = subtitle
        };
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var detailsUrl = $"https://anizone.to/anime/{animeId}/";
        var stream = await detailsUrl.GetStreamAsync();

        var doc = new HtmlDocument();
        doc.Load(stream);

        foreach (var li in doc.QuerySelectorAll("ul.grid li") ?? [])
        {
            var id = li.QuerySelector("a").GetAttributeValue("href", "")[detailsUrl.Length ..];
            var title = li.QuerySelector("h3").InnerHtml.Replace($"Episode {id} :", "").Trim();
            if (!float.TryParse(id, out var number))
            {
                continue;
            }

            yield return new Episode(this, animeId, id, number)
            {
                Info = new EpisodeInfo
                {
                    Titles = new Titles
                    {
                        English = title
                    }
                }
            };
        }
    }
}