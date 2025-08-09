using System.Diagnostics;

namespace TotoroNext.Anime.Abstractions.Models;

[DebuggerDisplay("{Name} ({Url})")]
public class VideoServer(string name, Uri url, IVideoExtractor? videoExtractor = null)
{
    public string Name { get; } = name;
    public Uri Url { get; } = url;
    public string? Quality { get; set; }
    public Dictionary<string, string> Headers { get; init; } = [];
    public string? Subtitle { get; set; }

    public async IAsyncEnumerable<VideoSource> Extract()
    {
        if (videoExtractor is null)
        {
            yield return new VideoSource
            {
                Url = Url,
                Quality = Quality,
                Headers = Headers,
                Subtitle = Subtitle
            };
        }
        else
        {
            await foreach (var stream in videoExtractor.Extract(Url))
            {
                yield return stream;
            }
        }
    }
}