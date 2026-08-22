using System.Text.Json;

namespace TotoroNext.Anime.Local.Mapping;

internal class Kitsu(IHttpClientFactory httpClientFactory)
{
	public async Task<int?> TryGetId(OfflineAnimeModel anime)
	{
		using var client = httpClientFactory.CreateClient();
		
		var url = $"https://kitsu.io/api/edge/mappings?filter[externalSite]=anilist/anime&filter[externalId]={anime.AnilistId}&include=item";
		var response = await client.GetAsync(url);
		if (!response.IsSuccessStatusCode)
		{
			return null;
		}

		var stream = await response.Content.ReadAsStreamAsync();
		using var doc = await JsonDocument.ParseAsync(stream);
		var root = doc.RootElement;

		// Kitsu JSON:API response structure parsing
		if (root.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
		{
			var firstMapping = dataArray[0];
			if (firstMapping.TryGetProperty("relationships", out var relationships) &&
				relationships.TryGetProperty("item", out var item) &&
				item.TryGetProperty("data", out var itemData) &&
				itemData.TryGetProperty("id", out var kitsuId))
			{
				var id = kitsuId.GetString();
				if(int.TryParse(id, out var intId))
				{
					return intId;
				}
			}
		}

		return null;
	}
}
