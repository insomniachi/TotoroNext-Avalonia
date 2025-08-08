using Flurl;
using Flurl.Http;
using HtmlAgilityPack;
using HtmlAgilityPack.CssSelectors.NetCore;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.AnimeHeaven;

public class AnimeProvider(IHttpClientFactory httpClientFactory) : IAnimeProvider
{
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        using var client = CreateClient();
        var stream = await client.Request("fastsearch.php")
                                 .AppendQueryParam("xhr", 1)
                                 .AppendQueryParam("s", query)
                                 .GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);

        var animeLinks = doc.DocumentNode.SelectNodes("//a[@class='ac']");
        foreach (var link in animeLinks)
        {
            var id = Url.Parse(link.GetAttributeValue("href", string.Empty)).QueryParams[0].Value.ToString() ?? "";
            var imgNode = link.SelectSingleNode(".//img[@class='coverimg']");
            var imageSrc = Url.Combine(client.BaseUrl, imgNode?.GetAttributeValue("src", string.Empty)) ?? "";
            var titleNode = link.SelectSingleNode(".//div[@class='fastname']");
            var title = titleNode?.InnerText.Trim() ?? "";

            yield return new SearchResult(this, id, title, new Uri(imageSrc));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        using var client = CreateClient();
        var stream = await client.Request("gate.php")
                                 .WithHeader(HeaderNames.Cookie, $"key={episodeId}")
                                 .GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);
        var source = doc.QuerySelector("source").GetAttributeValue("src",  string.Empty);
        if (string.IsNullOrEmpty(source))
        {
            yield break;
        }

        yield return new VideoServer("Default", new Uri(source));
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        using var client = CreateClient();
        var stream = await client.Request($"anime.php?{animeId}")
                           .GetStreamAsync();
        var doc = new HtmlDocument();
        doc.Load(stream);
        
        
        var gateLinks = doc.DocumentNode.SelectNodes("//a[contains(@href, 'gate.php')]");
        foreach (var link in gateLinks)
        {
            var episodeId = link.GetAttributeValue("id", string.Empty);
            var ep = link.QuerySelector(".watch2").InnerHtml;

            if (!float.TryParse(ep, out var episodeNumber))
            {
                continue;
            }

            yield return new Episode(this, animeId, episodeId, episodeNumber);
        }
    }

    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient("AnimeHeaven"));
    }
}