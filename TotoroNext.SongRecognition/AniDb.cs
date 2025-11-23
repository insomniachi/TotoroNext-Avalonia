using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Flurl;
using Flurl.Http;
using HtmlAgilityPack;

namespace TotoroNext.SongRecognition;

public static partial class AniDb
{
    internal static async Task<List<AniDbItem>> SearchSongs(string title)
    {
        string response;
        try
        {
            response = await "https://anidb.net/perl-bin/animedb.pl".SetQueryParams(new
                                                                    {
                                                                        show = "json",
                                                                        action = "search",
                                                                        type = "song",
                                                                        query = title
                                                                    })
                                                                    .WithHeader("x-lcontrol", "x-no-cache")
                                                                    .WithHeader("User-Agent", "anime-from-song")
                                                                    .GetStringAsync();
        }
        catch
        {
            return [];
        }

        var result = new List<AniDbItem>();
        var nodes = JsonNode.Parse(response)?.AsArray() ?? [];
        foreach (var node in nodes)
        {
            try
            {
                result.Add(node.Deserialize<AniDbItem>()!);
            }
            catch
            {
                // ignored
            }
        }

        return result
               .DistinctBy(x => x.Id)
               .Where(x => x.Title.Contains(title, StringComparison.InvariantCultureIgnoreCase))
               .Take(5)
               .ToList();
    }

    public static IEnumerable<AniDbItem> FindAnimeBySongId(int id)
    {
        var web = new HtmlWeb();
        HtmlDocument? doc;
        try
        {
            doc = web.Load($"https://anidb.net/song/{id}");
        }
        catch
        {
            yield break;
        }

        var nameNodes = doc.DocumentNode.SelectNodes("//table[@id='animelist']//tr/td[@class='name ']/a");

        foreach (var node in nameNodes?.OfType<HtmlNode>() ?? [])
        {
            if (!int.TryParse(node.GetAttributeValue("href", "").Split("/").LastOrDefault(), out var animeId))
            {
                continue;
            }

            yield return new AniDbItem
            {
                Id = animeId,
                Title = node.InnerHtml.Trim()
            };
        }
    }

    public static async IAsyncEnumerable<AniDbItem> FindAnimeFromSong(string title)
    {
        title = CleanTitleRegex().Replace(title, "");
        var songs = await SearchSongs(title.ToLower());

        if (songs.Any(x => x.Rating != "N/A (0)"))
        {
            songs = songs.Where(x => x.Rating != "N/A (0)").ToList();
        }

        foreach (var anime in songs.SelectMany(song => FindAnimeBySongId(song.Id)))
        {
            yield return anime;
        }
    }

    [GeneratedRegex(@"\\(.*?\\)")]
    private static partial Regex CleanTitleRegex();

    [DebuggerDisplay("{Title}")]
    [Serializable]
    public class AniDbItem
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("name")] public string Title { get; set; } = "";
        [JsonPropertyName("desc")] public string Rating { get; set; } = "";
    }
}