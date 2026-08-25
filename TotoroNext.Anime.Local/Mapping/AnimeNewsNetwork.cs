using System.Globalization;
using System.Xml.Linq;
using FuzzySharp;

namespace TotoroNext.Anime.Local.Mapping;

internal class AnimeNewsNetwork(IHttpClientFactory httpClientFactory)
{
    public const string XmlFile = "ann.xml";

    private static readonly string[] Formats =
    [
        "yyyy-MM-dd",
        "yyyy-MM",
        "yyyy"
    ];

    public List<AnnItemModel> Items { get; set; } = [];

    public async Task<int?> TryGetId(OfflineAnimeModel anime)
    {
        if (Items.Count == 0)
        {
            await ReadCache();
        }
        
        var candidates = Items;

        if (anime.StartDate.HasValue)
        {
            candidates = [.. Items.Where(x => IsMatchingDate(x.StartDate, anime.StartDate.Value))];
        }

        if (anime.EndDate.HasValue)
        {
            candidates = [.. Items.Where(x => IsMatchingDate(x.EndDate, anime.EndDate.Value))];
        }

        var filtered = candidates.Select(x => x.Name).ToList();
        var romajiMatch = Process.ExtractOne(anime.Title.Romaji, filtered);
        var bestMatch = romajiMatch;

        if (!string.IsNullOrEmpty(anime.Title.English))
        {
            var englishMatch = Process.ExtractOne(anime.Title.Romaji, filtered);
            if (bestMatch is not null && englishMatch is not null && bestMatch.Score < englishMatch.Score)
            {
                bestMatch = englishMatch;
            }
        }

        if (bestMatch is null)
        {
            return null;
        }

        return int.Parse(candidates.ElementAt(bestMatch.Index).Id);
    }

    public async Task DownloadDump()
    {
        if (File.Exists(XmlFile))
        {
            File.Delete(XmlFile);
        }
        
        const string url = "https://cdn.animenewsnetwork.com/encyclopedia/reports.xml?id=155&type=anime&nlist=all";
        using var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var fileStream = File.OpenWrite(XmlFile);
        await stream.CopyToAsync(fileStream);
        await fileStream.DisposeAsync();
    }

    private async Task ReadCache()
    {
        try
        {
            if (!File.Exists(XmlFile))
            {
                await DownloadDump();
            }
            
            await using var stream = File.OpenRead(XmlFile);
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

            foreach (var item in doc.Descendants("item"))
            {
                var id = item.Element("id")?.Value ?? string.Empty;
                var title = item.Element("name")?.Value ?? string.Empty;
                var type = item.Element("type")?.Value ?? string.Empty;
                var precision = item.Element("precision")?.Value ?? string.Empty;
                var season = precision.Replace(type, "").Trim();
                var vintage = item.Element("vintage")?.Value ?? string.Empty;

                if (!string.IsNullOrEmpty(season))
                {
                    season = $"Season {season.Trim()}";
                }

                var model = new AnnItemModel
                {
                    Id = id,
                    Name = $"{title} {season}"
                };

                if (!string.IsNullOrEmpty(vintage))
                {
                    (model.StartDate, model.EndDate) = ParseDateRange(vintage);
                }

                Items.Add(model);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public static (FuzzyDate? StartDate, FuzzyDate? EndDate) ParseDateRange(string? rawDateString)
    {
        if (string.IsNullOrWhiteSpace(rawDateString))
        {
            return (null, null);
        }

        var parts = rawDateString.Split([" to "], StringSplitOptions.RemoveEmptyEntries);

        var startDate = parts.Length > 0 ? ParseSingleDate(parts[0].Trim()) : null;
        var endDate = parts.Length > 1 ? ParseSingleDate(parts[1].Trim()) : null;

        return (startDate, endDate);
    }

    private static FuzzyDate? ParseSingleDate(string dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
        {
            return null;
        }

        var yearOnly = false;
        var yearAndMonthOnly = false;
        if (dateStr.Length == 4 && int.TryParse(dateStr, out _))
        {
            yearOnly = true;
            dateStr += "-01-01";
        }
        else if (dateStr.Length == 7 && dateStr[4] == '-')
        {
            yearAndMonthOnly = true;
            dateStr += "-01";
        }

        if (DateOnly.TryParseExact(dateStr, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            if (yearOnly)
            {
                return new FuzzyDate { Year = parsedDate.Year };
            }

            return yearAndMonthOnly
                ? new FuzzyDate { Year = parsedDate.Year, Month = parsedDate.Month }
                : new FuzzyDate { Year = parsedDate.Year, Month = parsedDate.Month, Day = parsedDate.Day };
        }

        return null;
    }

    private static bool IsMatchingDate(FuzzyDate? annDate, DateOnly date)
    {
        if (annDate is null)
        {
            return true;
        }

        var year = annDate.Year;
        var month = annDate.Month;
        var day = annDate.Day;

        if (year != 0 && year != date.Year)
        {
            return false;
        }

        if (month != 0 && month != date.Month)
        {
            return false;
        }

        return day == 0 || day == date.Day;
    }
}

internal class AnnItemModel
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public FuzzyDate? StartDate { get; set; }
    public FuzzyDate? EndDate { get; set; }
}

internal class FuzzyDate
{
    public int Year { get; init; }
    public int Month { get; init; }
    public int Day { get; init; }
}