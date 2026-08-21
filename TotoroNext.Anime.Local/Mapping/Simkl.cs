using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Local.Mapping;

internal static class Simkl
{
	public const string ClientId = "0a814ce1ee4819adcbcee198151e256f0700cc8c3976ad3084c8a329720124fc";
	private static readonly HttpClient Client = new(new SocketsHttpHandler { AllowAutoRedirect = false });

	public static async Task<int?> TryGetId(OfflineAnimeModel anime)
	{
		try
		{
			var url = $"https://api.simkl.com/redirect?to=Simkl&mal={anime.MyAnimeListId}&client_id={ClientId}";

			using var request = new HttpRequestMessage(HttpMethod.Get, url);

			var response = await Client.GetAsync(url);

			if (response.StatusCode is System.Net.HttpStatusCode.Found or System.Net.HttpStatusCode.MovedPermanently)
			{
				var redirectLocation = response.Headers.Location?.ToString();

				if (!string.IsNullOrEmpty(redirectLocation))
				{
					// The URL structure looks like: https://simkl.com/anime/54321/show-name
					// We can extract the numeric Simkl ID from the path segments.
					return ExtractSimklIdFromUrl(redirectLocation);
				}
			}

			return null;
		}
		catch
		{
			return null;
		}
	}

	private static int? ExtractSimklIdFromUrl(string url)
	{
		// Split the URL parts to isolate the ID segment
		Uri uri = new(url);
		string[] segments = uri.Segments;

		// Typically, the segment after /anime/, /tv/, or /movies/ is the ID
		for (int i = 0; i < segments.Length - 1; i++)
		{
			if (segments[i].Trim('/') == "anime" ||
				segments[i].Trim('/') == "tv" ||
				segments[i].Trim('/') == "movies")
			{
				string idCandidate = segments[i + 1].Trim('/');
				if (int.TryParse(idCandidate, out var id))
				{
					return id;
				}
			}
		}
		return null;
	}
}
