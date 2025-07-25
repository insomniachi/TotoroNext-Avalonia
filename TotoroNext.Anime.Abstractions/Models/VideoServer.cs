using System.Diagnostics;

namespace TotoroNext.Anime.Abstractions.Models;

[DebuggerDisplay("{Name} ({Url})")]
public class VideoServer(string name, Uri url, IVideoExtractor? videoExtractor = null)
{
    private readonly IVideoExtractor? _extractor = videoExtractor;

    public string Name { get; } = name;
    public Uri Url { get; } = url;
    public string? Quality { get; init; }
    public Dictionary<string, string> Headers { get; init; } = [];

    public async IAsyncEnumerable<VideoSource> Extract()
    {
        if (_extractor is null)
        {
            yield return new VideoSource
            {
                Url = Url,
                Quality = Quality,
                Headers = Headers
            };
        }
        else
        {
            await foreach (var stream in _extractor.Extract(Url))
            {
                yield return stream;
            }
        }
    }
}