using System.Text.Json;
using Flurl.Http;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Torrents.RealDebrid;

public class RealDebridService(IHttpClientFactory httpClientFactory) : IDebrid
{
    public async Task<Uri?> TryGetDirectDownloadLink(Uri magnet, CancellationToken ct)
    {
        try
        {
            using var client = new FlurlClient(httpClientFactory.CreateClient("RealDebrid"));
            var stream = await AddTorrent(client, magnet);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var id = doc.RootElement.GetProperty("id").GetString();

            _ = await client.Request("torrents", "selectFiles", id)
                            .PostUrlEncodedAsync(new
                            {
                                files = "1"
                            }, cancellationToken: ct)
                            .ReceiveStream();

            stream = await client.Request("torrents", "info", id)
                                 .GetStreamAsync(cancellationToken: ct);
            doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var links = doc.RootElement.GetProperty("links").EnumerateArray().ToList();

            if (links.Count == 0)
            {
                return null;
            }

            var link = links.First().GetString() ?? "";

            stream = await client.Request("unrestrict", "link")
                                 .PostUrlEncodedAsync(new
                                 {
                                     link
                                 }, cancellationToken: ct)
                                 .ReceiveStream();
            doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var downloadLink = doc.RootElement.GetProperty("download").GetString();

            await client.Request("torrents", "delete", id).DeleteAsync(cancellationToken: ct);

            return downloadLink is null ? magnet : new Uri(downloadLink);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    private static async Task<Stream> AddTorrent(FlurlClient client, Uri uri)
    {
        if (IsMagnetLink(uri))
        {
            return await client.Request("torrents", "addMagnet")
                               .PostUrlEncodedAsync(new
                               {
                                   magnet = uri.ToString()
                               }).ReceiveStream();
        }

        var stream = await uri.GetStreamAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return await client.Request("torrents", "addTorrent")
                           .PutAsync(new ByteArrayContent(memoryStream.ToArray()))
                           .ReceiveStream();
    }

    private static bool IsMagnetLink(Uri uri)
    {
        return uri.Scheme.Equals("magnet", StringComparison.OrdinalIgnoreCase);
    }
}