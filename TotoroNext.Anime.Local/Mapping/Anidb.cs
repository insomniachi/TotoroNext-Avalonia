using System.IO.Compression;
using System.Xml.Linq;

namespace TotoroNext.Anime.Local.Mapping;

internal class Anidb(IHttpClientFactory httpClientFactory)
{
    public const string GzFilePath = "anidb.xml.gz";
    public List<AnidbItem> Items { get; } = [];

    public async Task<long?> TryGetId(OfflineAnimeModel anime)
    {
        if (Items.Count == 0)
        {
            await ReadCache();
        }
        
        var titles = new[]
        {
            anime.Title.Native,
            anime.Title.Romaji,
            anime.Title.English
        }.OfType<string>();
        foreach (var title in titles)
        {
            if (Items.FirstOrDefault(x => x.Titles.Any(t => t.Equals(title, StringComparison.OrdinalIgnoreCase))) is { } match)
            {
                return match.Id;
            }
        }

        return null;
    }

    public async Task DownloadDump()
    {
        if (File.Exists(GzFilePath))
        {
            File.Delete(GzFilePath);
        }
        
        var client = httpClientFactory.CreateClient();
        const string url = "https://anidb.net/api/anime-titles.xml.gz";
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        await using var gzStream = await response.Content.ReadAsStreamAsync();
        var stream = File.OpenWrite(GzFilePath);
        await gzStream.CopyToAsync(stream);
        await stream.DisposeAsync();
    }

    private async Task ReadCache()
    {
        try
        {
            if (!File.Exists(GzFilePath))
            {
                await DownloadDump();
            }
            
            await using var fileStream = File.OpenRead(GzFilePath);
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

            var doc = await XDocument.LoadAsync(gzipStream, LoadOptions.None, CancellationToken.None);

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
    public int Id { get; init; }
    public List<string> Titles { get; init; } = [];
}