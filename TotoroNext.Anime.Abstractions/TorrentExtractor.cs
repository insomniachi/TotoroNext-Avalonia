using System.Runtime.CompilerServices;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class TorrentExtractor(IFactory<ITorrentStream, Guid> debridFactory) : ITorrentExtractor
{
    public async IAsyncEnumerable<VideoSource> Extract(Uri url, [EnumeratorCancellation] CancellationToken ct)
    {
        var debrid = debridFactory.CreateDefault();

        if (debrid is null)
        {
            yield return new VideoSource
            {
                Url = url,
                DownloaderType = DownloaderTypes.Torrent
            };
            yield break;
        }

        var directLink = await debrid.TryGetStreamUrl(url, ct);

        if (directLink is null || directLink == url)
        {
            yield return new VideoSource
            {
                Url = url,
                DownloaderType = DownloaderTypes.Torrent
            };
            yield break;
        }
        
        yield return new VideoSource
        {
            Url = directLink,
            DownloaderType = DownloaderTypes.Http
        };
    }
}