using Flurl;
using Flurl.Http;
using FuzzySharp;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.TokyoInsider;

public class AnimeProvider : IAnimeProvider
{
    public IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        var lowered = query.ToLower();
        return Catalog.Items
                      .Select(show => new { Show = show, Score = Fuzz.Ratio(lowered, show.Title.ToLower()) })
                      .Where(x => x.Score > 70)
                      .OrderByDescending(x => x.Score)
                      .Select(x => new SearchResult(this, x.Show.Id, x.Show.Title))
                      .ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var stream = await "https://www.tokyoinsider.com/"
                           .AppendPathSegment(animeId)
                           .GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);

        foreach (var node in doc.QuerySelectorAll(".episode").Reverse())
        {
            var link = node.QuerySelector("a");
            if (link is null)
            {
                continue;
            }

            if (!link.InnerHtml.Contains("episode"))
            {
                continue;
            }

            var id = link.GetAttributeValue("href", "");
            var epStr = id.Split('/').LastOrDefault();
            if (!float.TryParse(epStr, out var ep))
            {
                continue;
            }

            yield return new Episode(this, animeId, id, ep);
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var stream = await "https://www.tokyoinsider.com/"
                           .AppendPathSegment(episodeId)
                           .GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);

        foreach (var node in doc.QuerySelectorAll(".c_h2,.c_h2b"))
        {
            var link = node.QuerySelectorAll("a").ElementAt(1);
            if (link is null)
            {
                continue;
            }

            yield return new VideoServer(link.InnerHtml, new Uri(link.GetAttributeValue("href", "")));
        }
    }
}