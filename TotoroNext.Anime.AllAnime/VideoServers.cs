using System.Text.RegularExpressions;
using Flurl.Http;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.AllAnime;

internal static partial class VideoServers
{
    internal static async Task<VideoServer?> FromMp4Upload(string name, string url)
    {
        try
        {
            var response = await url.GetStringAsync();
            var match = Mp4JuicyServerRegex().Match(response.Replace(" ", "").Replace("\n", ""));

            return new VideoServer(name, new Uri(match.Groups[1].Value))
            {
                Headers =
                {
                    [HeaderNames.Referer] = "https://www.mp4upload.com/"
                },
                ContentType = "mp4"
            };
        }
        catch
        {
            return null;
        }
    }

    internal static VideoServer WithReferer(string name, string url, string referer)
    {
        return new VideoServer(name, new Uri(url))
        {
            Headers =
            {
                [HeaderNames.Referer] = referer
            }
        };
    }

    internal static VideoServer WithContentType(this VideoServer server, string type)
    {
        server.ContentType = type;
        return server;
    }

    [GeneratedRegex("video/mp4\\\",src:\\\"(https?://.*/video\\.mp4)\\\"")]
    private static partial Regex Mp4JuicyServerRegex();
}