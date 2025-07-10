using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface IVideoExtractor
{
    IAsyncEnumerable<VideoSource> Extract(Uri url);
}
