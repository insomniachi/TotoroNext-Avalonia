using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Miruro;

public class AnimeProvider(IHttpClientFactory httpClientFactory,
                           IModuleSettings<Settings> settings) : IAnimeProvider
{
    public const string BaseUrl = "https://www.miruro.tv";
    private static readonly byte[] PipeKey = HexToBytes("71951034f8fbcf53d89db52ceb3dc22c");

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var queryObject = BuildPipeQuery(("q", query),
                                         ("type", "ANIME"),
                                         ("limit", 20),
                                         ("offset", 0));

        var json = await SendPipeAsync(client, "search", queryObject, ct);
        var doc = JsonDocument.Parse(json);
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var anilistId = item.GetProperty("id").GetInt64();
            var malId = item.GetProperty("idMal").GetInt64();
            var title = item.GetProperty("title").GetProperty("romaji").GetString();
            var image = item.GetProperty("coverImage").GetProperty("large").GetString();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(image))
            {
                continue;
            }

            yield return new SearchResult(this, anilistId.ToString(), title, new Uri(image))
            {
                ExternalId = new AnimeId
                {
                    Anilist = anilistId,
                    MyAnimeList = malId
                }
            };
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var query = BuildPipeQuery(("anilistId", int.Parse(animeId)));
        var json = await SendPipeAsync(client, "episodes", query, ct);
        var root = JsonDocument.Parse(json);
        if (!root.RootElement.TryGetProperty("providers", out var providers))
        {
            yield break;
        }

        var preferredProvider = providers.GetProperty(settings.Value.PreferredProvider);
        foreach (var ep in ParseEpisodesFromProvider(preferredProvider))
        {
            yield return ep;
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        using var client = CreateClient();
        var node = JsonDocument.Parse(episodeId);
        var provider = settings.Value.PreferredProvider;
        var defaultSubType = settings.Value.PreferredSubType;
        var id = node.RootElement.GetProperty("episodeId").GetString();

        if (string.IsNullOrEmpty(id))
        {
            yield break;
        }

        await foreach (var server in GetStreamServersAsync(client, id, provider, defaultSubType, ct))
        {
            yield return server;
        }
    }
    
    public List<ModuleOptionItem> GetOptions()
    {
        return settings.Value.ToModuleOptions();
    }

    public void UpdateOptions(List<ModuleOptionItem> options)
    {
        settings.Value.UpdateValues(options);
    }


    private static async IAsyncEnumerable<VideoServer> GetStreamServersAsync(
        FlurlClient client,
        string episodeId,
        string provider,
        string category,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var query = BuildPipeQuery(
                                   ("episodeId", episodeId),
                                   ("provider", provider),
                                   ("category", category)
                                  );
        var json = await SendPipeAsync(client, "sources", query, cancellationToken);
        var root = JsonDocument.Parse(json);

        foreach (var streamNode in root.RootElement.GetProperty("streams").EnumerateArray())
        {
            var type = streamNode.GetProperty("type").GetString() ?? "";
            if (!type.Equals("hls", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var url = streamNode.GetProperty("url").GetString();
            var referer = streamNode.GetProperty("referer").GetString();
            var hasQuality = streamNode.TryGetProperty("quality", out var qualityNode);
            var hasServer = streamNode.TryGetProperty("server", out var serverNode);
            
            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            var title = "";
            if (hasQuality)
            {
                title = qualityNode.GetString();
            }
            else if (hasServer)
            {
                title = serverNode.GetString();
            }
            
            var server = new VideoServer(title ?? "Default", new Uri(url));

            if (!string.IsNullOrEmpty(referer))
            {
                server.Headers.TryAdd(HeaderNames.Referer, referer);
            }

            yield return server;
        }
    }

    private List<Episode> ParseEpisodesFromProvider(JsonElement providerData)
    {
        var episodesObject = providerData.GetProperty("episodes");

        string[] subTypes = settings.Value.PreferredProvider == "bee" ? ["ssub", "sub", "dub"] : ["sub", "dub"];
        var episodeMap = new Dictionary<float, Dictionary<string, string>>();
        var titles = new Dictionary<float, string>();

        foreach (var subType in subTypes)
        {
            foreach (var episodeNode in episodesObject.GetProperty(subType).EnumerateArray())
            {
                var number = episodeNode.GetProperty("number").GetSingle();
                var id = episodeNode.GetProperty("id").GetString();

                if (number <= 0 || string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (!episodeMap.TryGetValue(number, out var subTypeIds))
                {
                    subTypeIds = new Dictionary<string, string>();
                    episodeMap[number] = subTypeIds;
                }

                subTypeIds[subType] = id;
                if (!titles.ContainsKey(number))
                {
                    titles[number] = episodeNode.GetProperty("title").GetString() ?? "";
                }
            }
        }

        return episodeMap
               .Select(pair =>
               {
                   titles.TryGetValue(pair.Key, out var title);
                   return BuildEpisode(pair.Key, title, pair.Value);
               })
               .Where(x => x is not null)
               .Select(x => x!)
               .ToList();
    }


    private FlurlClient CreateClient()
    {
        return new FlurlClient(httpClientFactory.CreateClient($"{Module.Id}"));
    }

    private static async ValueTask<string> SendPipeAsync(
        FlurlClient client,
        string path,
        JsonObject query,
        CancellationToken cancellationToken
    )
    {
        var payload = new JsonObject
        {
            ["path"] = path,
            ["method"] = "GET",
            ["query"] = query,
            ["body"] = null,
            ["version"] = "0.2.0"
        };
        var encoded = Base64UrlEncode(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        using var request = new HttpRequestMessage(
                                                   HttpMethod.Get,
                                                   $"{BaseUrl}/api/secure/pipe?e={encoded}"
                                                  );

        request.Version = HttpVersion.Version20;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

        // Miruro's Cloudflare edge enforces a WAF rule that 403s pipe-API
        // requests whose headers don't match a real Chrome CORS fetch.
        foreach (var header in ApiFingerprintHeaders(BaseUrl))
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        using var response = await client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var obfuscated = response.Headers.TryGetValues("x-obfuscated", out var values)
            ? values.FirstOrDefault()
            : "1";

        return obfuscated == "2" ? DecryptPipeResponse(body) : body.Trim();
    }


    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string DecryptPipeResponse(string body)
    {
        var decoded = Base64UrlDecode(body.Trim());
        for (var i = 0; i < decoded.Length; i++)
        {
            decoded[i] = (byte)(decoded[i] ^ PipeKey[i % PipeKey.Length]);
        }

        using var input = new MemoryStream(decoded);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        return Convert.FromBase64String(base64);
    }

    private static byte[] HexToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }

    private static JsonObject BuildPipeQuery(params (string Key, object? Value)[] pairs)
    {
        var query = new JsonObject();
        foreach (var (key, value) in pairs)
        {
            if (value is null)
            {
                continue;
            }

            query[key] = value switch
            {
                int intValue => intValue,
                long longValue => longValue,
                double doubleValue => doubleValue,
                float floatValue => floatValue,
                bool boolValue => boolValue,
                _ => value.ToString()
            };
        }

        return query;
    }


    private Episode? BuildEpisode(float number, string? title, Dictionary<string, string> subTypeIds)
    {
        var defaultSubType = subTypeIds.ContainsKey(settings.Value.PreferredSubType)
            ? settings.Value.PreferredSubType
            : "";

        if (string.IsNullOrWhiteSpace(defaultSubType))
        {
            return null;
        }
        
        var episodeIdObject = new JsonObject
        {
            ["episodeId"] = subTypeIds[defaultSubType],
        };

        return new Episode(this, "", episodeIdObject.ToJsonString(), number)
        {
            Info = new EpisodeInfo
            {
                Titles = new Titles
                {
                    English = title?.Trim() ?? ""
                }
            }
        };
    }
    
    private const string ChromeMajorVersion = "148";

    public static Dictionary<string, string> ApiFingerprintHeaders(
        string origin,
        string? referer = null,
        bool sameOrigin = true
    )
    {
        return new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Accept-Language"] = "en-US,en;q=0.9",
            ["User-Agent"] = Http.UserAgent,
            ["Sec-Ch-Ua"] = $"\"Chromium\";v=\"{ChromeMajorVersion}\", \"Not_A Brand\";v=\"24\", \"Google Chrome\";v=\"{ChromeMajorVersion}\"",
            ["Sec-Ch-Ua-Mobile"] = "?0",
            ["Sec-Ch-Ua-Platform"] = "\"Windows\"",
            ["Sec-Fetch-Dest"] = "empty",
            ["Sec-Fetch-Mode"] = "cors",
            ["Sec-Fetch-Site"] = sameOrigin ? "same-origin" : "same-site",
            ["Origin"] = origin,
            ["Referer"] = referer ?? $"{origin}/"
        };
    }
}