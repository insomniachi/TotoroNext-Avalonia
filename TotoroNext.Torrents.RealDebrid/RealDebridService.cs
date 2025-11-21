using System.Text.Json;
using Flurl.Http;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Torrents.RealDebrid;

public class RealDebridService(IHttpClientFactory httpClientFactory) : IDebrid
{
    public async Task<Uri?> TryGetDirectDownloadLink(Uri magnet)
    {
        try
        {
            using var client = new FlurlClient(httpClientFactory.CreateClient("RealDebrid"));
            var stream = await client.Request("torrents", "addMagnet")
                                     .PostUrlEncodedAsync(new
                                     {
                                         magnet = magnet.ToString()
                                     }).ReceiveStream();
            var doc = await JsonDocument.ParseAsync(stream);
            var id = doc.RootElement.GetProperty("id").GetString();

            _ = await client.Request("torrents", "selectFiles", id)
                            .PostUrlEncodedAsync(new
                            {
                                files = "1"
                            }).ReceiveStream();

            stream = await client.Request("torrents", "info", id)
                                 .GetStreamAsync();
            doc = await JsonDocument.ParseAsync(stream);
            var link = doc.RootElement.GetProperty("links").EnumerateArray().FirstOrDefault().GetString() ?? "";

            stream = await client.Request("unrestrict", "link")
                                 .PostUrlEncodedAsync(new
                                 {
                                     link
                                 }).ReceiveStream();
            doc = await JsonDocument.ParseAsync(stream);
            var downloadLink = doc.RootElement.GetProperty("download").GetString();

            await client.Request("torrents", "delete", id).DeleteAsync();
            
            return downloadLink is null ? magnet : new Uri(downloadLink);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}