using System.Net;

namespace TotoroNext.Anime.Local.Mapping;

internal static class Simkl
{
    private static readonly HttpClient Client = new(new SocketsHttpHandler { AllowAutoRedirect = false });

    public static async Task<int?> TryGetId(OfflineAnimeModel anime, string clientId)
    {
        try
        {
            var url = $"https://api.simkl.com/redirect?to=Simkl&mal={anime.MyAnimeListId}&client_id={clientId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await Client.GetAsync(url);
            if (response.StatusCode is not (HttpStatusCode.Found or HttpStatusCode.MovedPermanently))
            {
                return null;
            }

            var redirectLocation = response.Headers.Location?.ToString();
            return !string.IsNullOrEmpty(redirectLocation) ? ExtractSimklIdFromUrl(redirectLocation) : null;
        }
        catch
        {
            return null;
        }
    }

    private static int? ExtractSimklIdFromUrl(string url)
    {
        Uri uri = new(url);
        var segments = uri.Segments;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Trim('/') != "anime" &&
                segments[i].Trim('/') != "tv" &&
                segments[i].Trim('/') != "movies")
            {
                continue;
            }

            var idCandidate = segments[i + 1].Trim('/');
            if (int.TryParse(idCandidate, out var id))
            {
                return id;
            }
        }

        return null;
    }
}