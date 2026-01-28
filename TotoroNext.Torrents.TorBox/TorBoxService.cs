using System.Text.Json.Serialization;
using Flurl.Http;
using JetBrains.Annotations;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Torrents.TorBox;

internal class TorBoxService(
    IHttpClientFactory httpClientFactory,
    IModuleSettings<Settings> settings) : IDebrid
{
    private readonly FlurlClient _client = new(httpClientFactory.CreateClient("TorBox"));

    public async Task<Uri?> TryGetDirectDownloadLink(Uri uri, CancellationToken ct)
    {
        try
        {
            var magnet = await TorrentHelper.TorrentToMagnet(uri);
            var response = await _client.Request("torrents", "createtorrent")
                                        .PostUrlEncodedAsync(new
                                        {
                                            magnet,
                                            add_only_if_cached = true
                                        }, cancellationToken: ct)
                                        .ReceiveJson<TorBoxResponse<CreateTorrentData>>();

            if (response is { Success: false } or { Data: null })
            {
                return null;
            }

            var dlResponse = await _client.Request("torrents", "requestdl")
                                          .SetQueryParam("token", settings.Value.Token)
                                          .SetQueryParam("torrent_id", response.Data.TorrentId)
                                          .GetJsonAsync<TorBoxResponse<string>>(cancellationToken: ct);

            var result = dlResponse is { Success: false } or { Data: null } ? null : new Uri(dlResponse.Data);
            
            await _client.Request("torrents", "controltorrent")
                         .PostJsonAsync(new
                         {
                             torrent_id = response.Data.TorrentId,
                             operation = "delete"
                         }, cancellationToken: ct);

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }

    public async Task DeleteAllTorrents()
    {
        await _client.Request("torrents", "controltorrent")
                     .PostJsonAsync(new
                     {
                         all = true,
                         operation = "delete"
                     });
    }

    [Serializable]
    private class TorBoxResponse<T>
    {
        [JsonPropertyName("success")] public bool Success { get; set; }

        [JsonPropertyName("error")]
        [UsedImplicitly]
        public string? Error { get; set; }

        [JsonPropertyName("detail")]
        [UsedImplicitly]
        public string Detail { get; set; } = "";

        [JsonPropertyName("data")] public T? Data { get; set; }
    }

    [Serializable]
    private class CreateTorrentData
    {
        [JsonPropertyName("torrent_id")] public int TorrentId { get; set; }
    }
}