using System.Runtime.CompilerServices;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class TorrentExtractor(IFactory<IDebrid, Guid> debridFactory) : ITorrentExtractor
{
    public async IAsyncEnumerable<VideoSource> Extract(Uri url, [EnumeratorCancellation] CancellationToken ct)
    {
        var debrid = debridFactory.CreateDefault();

        if (debrid is null)
        {
            yield return new VideoSource
            {
                Url = url
            };
            yield break;
        }

        var directLink = await debrid.TryGetDirectDownloadLink(url, ct);
        yield return new VideoSource
        {
            Url = directLink ?? url
        };
    }
}