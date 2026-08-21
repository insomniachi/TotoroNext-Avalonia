using System.Globalization;
using System.Xml.Linq;
using FuzzySharp;

namespace TotoroNext.Anime.Local.Mapping;

internal class AnimeNewsNetwork(IHttpClientFactory httpClientFactory)
{
	public const string XmlFile = "ann.xml";

	public List<AnnItemModel> Items { get; set; } = [];

	public int? TryGetId(OfflineAnimeModel anime)
	{
		IEnumerable<AnnItemModel> candidates = Items;

		if(anime.StartDate.HasValue)
		{
			candidates = Items.Where(x => IsMatchingDate(x.StartDate, anime.StartDate.Value));
		}

		if(anime.EndDate.HasValue)
		{
			candidates = Items.Where(x => IsMatchingDate(x.EndDate, anime.EndDate.Value));
		}

		candidates = candidates.ToList();

		var filtered = candidates.Select(x => x.Name).ToList();
		var romajiMatch = Process.ExtractOne(anime.Title.Romaji, filtered);
		var bestMatch = romajiMatch;

		if (!string.IsNullOrEmpty(anime.Title.English))
		{
			var englishMatch = Process.ExtractOne(anime.Title.Romaji, filtered);
			if(bestMatch is not null && englishMatch is not null && bestMatch.Score < englishMatch.Score)
			{
				bestMatch = englishMatch;
			}
		}

		if(bestMatch is null)
		{
			return null;
		}

		return int.Parse(candidates.ElementAt(bestMatch.Index).Id);
	}

	public async Task CacheAnnDirectoryAsync()
	{
		if(File.Exists(XmlFile))
		{
			await ReadCache();
			return;
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
		await ReadCache();
	}

	private async Task ReadCache()
	{
		try
		{
			await using var stream = File.OpenRead(XmlFile);
			var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

			// Iterate through the report rows provided directly by ANN
			// Each row typically contains the ANN id, title, and type
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
					Name = $"{title} {season}",
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
	private static readonly string[] Formats = {
		"yyyy-MM-dd",
		"yyyy-MM",
		"yyyy"
	};

	public static (FuzzyDate? StartDate, FuzzyDate? EndDate) ParseDateRange(string? rawDateString)
	{
		if (string.IsNullOrWhiteSpace(rawDateString))
			return (null, null);

		// Handle ranges by splitting on " to " (case-insensitive or exact)
		string[] parts = rawDateString.Split([" to "], StringSplitOptions.RemoveEmptyEntries);

		FuzzyDate? startDate = parts.Length > 0 ? ParseSingleDate(parts[0].Trim()) : null;
		FuzzyDate? endDate = parts.Length > 1 ? ParseSingleDate(parts[1].Trim()) : null;

		return (startDate, endDate);
	}

	private static FuzzyDate? ParseSingleDate(string dateStr)
	{
		if (string.IsNullOrWhiteSpace(dateStr))
		{
			return null;
		}

		bool yearOnly = false;
		bool yearAndMonthOnly = false;
		// 1. Normalize partial formats by appending default values
		// If it's just "yyyy" (4 digits) -> "yyyy-01-01"
		if (dateStr.Length == 4 && int.TryParse(dateStr, out _))
		{
			yearOnly = true;
			dateStr += "-01-01";
		}
		// If it's "yyyy-MM" (7 characters like "2026-03") -> "yyyy-MM-01"
		else if (dateStr.Length == 7 && dateStr[4] == '-')
		{
			yearAndMonthOnly = true;
			dateStr += "-01";
		}

		// 2. Try parsing using DateOnly.TryParseExact
		if (DateOnly.TryParseExact(dateStr, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsedDate))
		{
			if (yearOnly)
			{
				return new FuzzyDate { Year = parsedDate.Year };
			}
			else if (yearAndMonthOnly)
			{
				return new FuzzyDate { Year = parsedDate.Year, Month = parsedDate.Month };
			}
			else
			{
				return new FuzzyDate { Year =parsedDate.Year, Month = parsedDate.Month, Day = parsedDate.Day };
			}
		}

		return null;
	}

	private static bool IsMatchingDate(FuzzyDate? annDate, DateOnly date)
	{
		if(annDate is null)
		{
			return false;
		}

		var year = annDate.Year;
		var month = annDate.Month;
		var day = annDate.Day;

		if(year != 0 && year != date.Year)
		{
			return false;
		}

		if(month != 0 && month != date.Month)
		{
			return false;
		}

		if(day != 0 && day != date.Day)
		{
			return false;
		}

		return true;
	}
}

class AnnItemModel
{
	public string Id { get; set; } = "";
	public string Name { get; set; } = "";
	public FuzzyDate? StartDate { get; set; }
	public FuzzyDate? EndDate { get; set; }
}

class FuzzyDate
{
	public int Year { get; set; }
	public int Month { get; set; }
	public int Day { get; set; }	
}
