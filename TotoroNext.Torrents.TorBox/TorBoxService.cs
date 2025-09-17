using System.Text.Json.Serialization;
using Flurl.Http;
using JetBrains.Annotations;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Torrents.TorBox;

internal class TorBoxService(IHttpClientFactory httpClientFactory) : IDebrid
{
    private readonly FlurlClient _client = new(httpClientFactory.CreateClient("TorBox"));

    public async Task DeleteAllTorrents()
    {
        await _client.Request("torrents", "controltorrent")
                     .PostJsonAsync(new
                     {
                         all = true,
                         operation = "delete"
                     });
    }

    public async Task<Uri?> TryGetDirectDownloadLink(Uri magnet)
    {
        try
        {
            var response = await _client.Request("torrents", "createtorrent")
                                        .PostUrlEncodedAsync(new
                                        {
                                            magnet = magnet.ToString(),
                                            add_only_if_cached = true
                                        })
                                        .ReceiveJson<TorBoxResponse<CreateTorrentData>>();

            if (response is { Success: false } or { Data: null })
            {
                return null;
            }

            var token = _client.Headers.FirstOrDefault(x => x.Name == "Authorization") is { } authHeader
                ? authHeader.Value.Replace("Bearer ", "")
                : null;

            var dlResponse = await _client.Request("torrents", "requestdl")
                                          .SetQueryParam("token", token)
                                          .SetQueryParam("torrent_id", response.Data.TorrentId)
                                          .GetJsonAsync<TorBoxResponse<string>>();

            return dlResponse is { Success: false } or { Data: null } ? null : new Uri(dlResponse.Data);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
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