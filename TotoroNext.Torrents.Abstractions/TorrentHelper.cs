using System.Text;
using Flurl.Http;
using MonoTorrent;

namespace TotoroNext.Torrents.Abstractions;

public static class TorrentHelper
{
    public static async Task<string> TorrentToMagnet(Uri uri)
    {
        var stream = await uri.GetStreamAsync();
        var path = Path.GetTempFileName();
        await using (var fileStream = File.Create(path))
        {
            await stream.CopyToAsync(fileStream);
        }
        var torrent = await Torrent.LoadAsync(path);
        File.Delete(path);
        
        var sb = new StringBuilder();
        sb.Append("magnet:?xt=urn:btih:");
        sb.Append(torrent.InfoHashes.V1OrV2.ToHex());
        if (!string.IsNullOrEmpty(torrent.Name))
        {
            sb.Append("&dn=");
            sb.Append(Uri.EscapeDataString(torrent.Name));
        }

        foreach (var tier in torrent.AnnounceUrls)
        {
            foreach (var tracker in tier)
            {
                sb.Append("&tr=");
                sb.Append(Uri.EscapeDataString(tracker));
            }
        }

        return sb.ToString();
    }
}