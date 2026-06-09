using MonoTorrent;
using MonoTorrent.Client;
using MonoTorrent.Streaming;
using TotoroNext.Module;

namespace TotoroNext.Torrents.Abstractions;

public class MonoTorrentStream(IHttpClientFactory httpClientFactory,
                               ClientEngine engine) : ITorrentStream
{
    public static readonly Guid MonoTorrentStreamId = Guid.Parse("3b6fb775-4b32-4cd2-ba13-9dd0551af2b8");
    private static IHttpStream? _stream;

    public async Task<Uri?> TryGetStreamUrl(Uri uri, CancellationToken ct)
    {
        _stream?.Dispose();
        
        var path = Path.GetTempFileName();
        var client = httpClientFactory.CreateClient();
        var torrent = await Torrent.LoadAsync(client, uri, path);
        File.Delete(path);
        
        var manager = await engine.AddStreamingAsync(torrent, FileHelper.GetPath("Downloads"));
        await manager.StartAsync();
        await manager.WaitForMetadataAsync(ct);
        
        _stream = await manager.StreamProvider!.CreateHttpStreamAsync(manager.Files[0], ct);
        return new Uri(_stream.FullUri);
    }
}