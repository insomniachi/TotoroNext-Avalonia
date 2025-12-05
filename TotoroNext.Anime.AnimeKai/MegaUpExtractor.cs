using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.AnimeKai;

public class MegaUpExtractor : IVideoExtractor
{
    public async IAsyncEnumerable<VideoSource> Extract(Uri url)
    {
        var token = url.Segments.LastOrDefault();
        if (string.IsNullOrEmpty(token))
        {
            yield break;
        }

        var megaTokenResponse = await $"{url.Scheme}://{url.Host}"
                                      .AppendPathSegment("media")
                                      .AppendPathSegment(token)
                                      .WithHeader(HeaderNames.UserAgent, Http.UserAgent)
                                      .WithHeader(HeaderNames.Referer, url)
                                      .GetJsonAsync<ResultResponse<string>>();

        var megaResult = await "https://enc-dec.app/api/dec-mega"
                               .PostJsonAsync(new
                               {
                                   text = megaTokenResponse.Result,
                                   agent = Http.UserAgent
                               }).ReceiveJson<ResultResponse<MegaUpResult>>();

        if (megaResult.Result is not { Sources.Capacity: > 0 })
        {
            yield break;
        }

        var subtitles = megaResult.Result.Tracks.FirstOrDefault(x => x.Kind == "captions" && x.File.EndsWith(".vtt"))?.File;
        foreach (var sources in megaResult.Result.Sources)
        {
            yield return new VideoSource
            {
                Url = new Uri(sources.File),
                Headers =
                {
                    { HeaderNames.Referer, url.ToString() }
                },
                Subtitle = subtitles
            };
        }
    }
}

[Serializable]
internal class MegaUpResult
{
    [JsonPropertyName("sources")] public List<MegaUpSource> Sources { get; set; } = [];
    [JsonPropertyName("tracks")] public List<MegaUpTrack> Tracks { get; set; } = [];
    [JsonPropertyName("download")] public string Download { get; set; } = string.Empty;
}

[Serializable]
internal class MegaUpSource
{
    [JsonPropertyName("file")] public string File { get; set; } = string.Empty;
}

[Serializable]
internal class MegaUpTrack
{
    [JsonPropertyName("file")] public string File { get; set; } = string.Empty;
    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;
    [JsonPropertyName("kind")] public string Kind { get; set; } = string.Empty;
    [JsonPropertyName("default")] public bool Default { get; set; }
}