using MonoTorrent;
using MonoTorrent.Client;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions.Downloading;

public class MonoTorrentDownloader(
    ClientEngine engine,
    IHttpClientFactory httpClientFactory) : IDownloader
{
    public async Task<IDownloadOperation?> CreateDownload(AnimeModel anime, Episode episode, VideoSource source, string filepath)
    {
        using var client = httpClientFactory.CreateClient();
        var dir = Path.GetDirectoryName(filepath)!;
        var path = Path.GetTempFileName();
        var torrent = await Torrent.LoadAsync(client, source.Url, path);
        var manager = await engine.AddAsync(torrent, dir);

        return new MonotorrentDownloadOperation(manager, path, anime, episode, dir)
        {
            Link = source.Url,
            FileName = filepath
        };
    }
}