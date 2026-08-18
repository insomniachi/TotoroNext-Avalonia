using Banned.Qbittorrent;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;
using AddTorrentRequest = Banned.Qbittorrent.Models.Requests.AddTorrentRequest;

namespace TotoroNext.Torrents.Qbittorrent;

internal class TorrentStream(IModuleSettings<Settings> settings) : ITorrentStream
{
    public async Task<Uri?> TryGetStreamUrl(Uri torrentUri, CancellationToken ct)
    {
        using var client = await CreateClient();

        var existingTorrents = await client.Torrent.GetTorrentInfos();
        var existingHashes = new HashSet<string>(existingTorrents.Select(t => t.Hash ?? ""));

        await client.Torrent.AddTorrent(new AddTorrentRequest
        {
            Urls = [torrentUri.ToString()],
            SavePath = FileHelper.GetPath("Torrents"),
            SequentialDownloadEnabled = true,
            FirstLastPiecePriorityEnabled = true
        });

        var currentTorrents = await client.Torrent.GetTorrentInfos();
        var currentTorrent = currentTorrents.FirstOrDefault(t => !existingHashes.Contains(t.Hash ?? ""));

        if (currentTorrent == null)
        {
            Console.WriteLine("Error: Torrent could not be found after adding.");
            return null;
        }

        var torrentHash = currentTorrent.Hash!;
        var savePath = currentTorrent.SavePath!;

        string? mediaFilePath = null;
        while (string.IsNullOrEmpty(mediaFilePath))
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            var files = await client.Torrent.GetTorrentFiles(torrentHash) ?? [];

            var videoFile = files.FirstOrDefault(f =>
                                                     f.Name.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                                                     f.Name.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                                                     f.Name.EndsWith(".avi", StringComparison.OrdinalIgnoreCase));

            if (videoFile != null)
            {
                mediaFilePath = Path.Combine(savePath, videoFile.Name);
            }
        }

        return new Uri(mediaFilePath);
    }


    private async Task<QBittorrentClient> CreateClient()
    {
        return await QBittorrentClient.Create(settings.Value.Url, settings.Value.Username, settings.Value.Password);
    }
}