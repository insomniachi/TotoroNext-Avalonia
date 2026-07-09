using MonoTorrent;
using MonoTorrent.Client;
using TotoroNext.Anime.Abstractions.Downloading;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public class MonoTorrentDownloader(
    ClientEngine engine,
    IHttpClientFactory httpClientFactory) : IDownloader
{
    public async Task<IDownloadOperation?> CreateDownload(AnimeModel anime, Episode episode, VideoServer server, string filepath)
    {
        using var client = httpClientFactory.CreateClient();
        var dir = Path.GetDirectoryName(filepath)!;
        var path = Path.GetTempFileName();
        var torrent = await Torrent.LoadAsync(client, server.Url, path);
        var manager = await engine.AddAsync(torrent, dir);

        return new MonotorrentDownloadOperation(manager, path, episode, dir)
        {
            Link = server.Url,
            FileName = filepath
        };
    }
}