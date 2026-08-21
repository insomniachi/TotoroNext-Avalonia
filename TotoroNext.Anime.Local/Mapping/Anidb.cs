using System.IO.Compression;
using System.Xml.Linq;

namespace TotoroNext.Anime.Local.Mapping;

internal class Anidb(IHttpClientFactory httpClientFactory)
{
    public const string GzFilePath = "anidb.xml.gz";
    public List<AnidbItem> Items { get; } = [];

    public long? TryGetId(OfflineAnimeModel anime)
    {
        var titles = new[] { anime.Title.Native, anime.Title.Romaji, anime.Title.English }.Where(x => !string.IsNullOrEmpty(x));
        foreach (var title in titles)
        {
            if (Items.FirstOrDefault(x => x.Titles.Any(t => t.Equals(title, StringComparison.OrdinalIgnoreCase))) is { } match)
            {
                return match.Id;
            }
        }

        return null;
    }

    public async Task CacheAniDbTitlesAsync()
    {
        if (File.Exists(GzFilePath))
        {
            await ReadCache();
            return;
        }

        var client = httpClientFactory.CreateClient();

        // AniDB official public anime titles dump URL
        const string url = "https://anidb.net/api/anime-titles.xml.gz";

        Console.WriteLine("Downloading AniDB titles dump...");

        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        // 1. Read the network stream and decompress GZip (.gz) on the fly
        await using var gzStream = await response.Content.ReadAsStreamAsync();
        var stream = File.OpenWrite(GzFilePath);
        await gzStream.CopyToAsync(stream);
        await stream.DisposeAsync();
        await ReadCache();
    }

    private async Task ReadCache()
    {
        try
        {
            await using var fileStream = File.OpenRead(GzFilePath);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

            var doc = await XDocument.LoadAsync(gzipStream, LoadOptions.None, CancellationToken.None);

            // 4. Parse each anime node and its alternative titles
            foreach (var anime in doc.Descendants("anime"))
            {
                var aid = anime.Attribute("aid")?.Value;
                if (string.IsNullOrEmpty(aid))
                {
                    continue;
                }

                var titles = new List<string>();
                foreach (var titleElement in anime.Elements("title"))
                {
                    var title = titleElement.Value;

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(titleElement.Value))
                    {
                        titles.Add(titleElement.Value);
                    }
                }

                // Create a compact cache entry object
                Items.Add(new AnidbItem { Id = int.Parse(aid), Titles = titles });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}

internal class AnidbItem
{
    public int Id { get; set; }
    public List<string> Titles { get; set; } = [];
}