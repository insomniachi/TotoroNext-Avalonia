using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl;
using Flurl.Http;
using FlurlGraphQL;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AllAnime;

internal class AnimeProvider(IModuleSettings<Settings> settings) : IAnimeProvider
{
    private const string DecryptSecret = "Xot36i3lK3";
    private const int DecryptTagLength = 128; // bits
    private static readonly string[] XorKeys =
    [
        "allanimenews",
        "1234567890123456789",
        "1234567890123456789012345",
        "s5feqxw21",
        "feqx1"
    ];

    private const string GraphQlReferer = @"https://youtu-chan.com";

    // Pre-compute cumulative XOR mask for each key (XOR of all char codes)
    private static readonly int[] XorMasks = XorKeys
                                              .Select(key => key.Aggregate(0, (mask, ch) => mask ^ ch))
                                              .ToArray();
    
    public async IAsyncEnumerable<SearchResult> SearchAsync(string query, [EnumeratorCancellation] CancellationToken ct)
    {
        var jObject = await GraphQl.Api
                                   .WithGraphQLQuery(GraphQl.SearchQuery)
                                   .SetGraphQLVariables(new
                                   {
                                       search = new
                                       {
                                           allowAdult = true,
                                           allowUnknown = true,
                                           query
                                       },
                                       limit = 40
                                   })
                                   .PostGraphQLQueryAsync(ct)
                                   .ReceiveGraphQLRawSystemTextJsonResponse();

        foreach (var item in jObject?["shows"]?["edges"]?.AsArray().OfType<JsonObject>() ?? [])
        {
            ct.ThrowIfCancellationRequested();

            var title = $"{item["name"]}";
            var id = $"{item["_id"]}";
            Uri? image = null;
            long malId = 0;
            long anilistId = 0;
            try
            {
                image = new Uri($"{item["thumbnail"]}");
                malId = long.Parse($"{item["malId"]}");
                anilistId = long.Parse($"{item["aniListId"]}");
            }
            catch
            {
                // ignored
            }

            yield return new SearchResult(this, id, title, image)
            {
                ExternalId = new AnimeId
                {
                    MyAnimeList = malId,
                    Anilist = anilistId
                }
            };
        }
    }
    
    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var jObject = await GraphQl.Api
                                   .WithGraphQLQuery(GraphQl.ShowQuery)
                                   .SetGraphQLVariable("showId", animeId)
                                   .PostGraphQLQueryAsync(ct)
                                   .ReceiveGraphQLRawSystemTextJsonResponse();

        if (jObject?["show"]?["availableEpisodesDetail"] is not JsonObject episodeDetails)
        {
            yield break;
        }

        var details = episodeDetails.Deserialize<EpisodeDetails>();

        foreach (var episode in Enumerable.Reverse(GetEpisodeDetails(details, settings.Value.TranslationType)))
        {
            ct.ThrowIfCancellationRequested();
            yield return new Episode(this, animeId, episode, float.Parse(episode));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId, [EnumeratorCancellation] CancellationToken ct)
    {
        var variables = JsonSerializer.Serialize(new
        {
            showId = animeId,
            translationType = GetTranslationType(settings.Value.TranslationType),
            episodeString = episodeId
        });

        var extensions = JsonSerializer.Serialize(new
        {
            persistedQuery = new
            {
                version = 1,
                sha256Hash = "d405d0edd690624b66baba3068e0edc3ac90f1597d898a1ec8db4e5c43c00fec"
            }
        });

        var stream = await GraphQl.Api
                               .AppendQueryParam("variables", variables)
                               .AppendQueryParam("extensions", extensions)
                               .WithHeader(HeaderNames.Referer, GraphQlReferer)
                               .GetStreamAsync(cancellationToken:ct);
        
        var jsonNode = await JsonNode.ParseAsync(stream, cancellationToken:ct);
        

        var encrypted = jsonNode?["data"]?["tobeparsed"]?.GetValue<string>();
        if (encrypted  is not null)
        {
            var decrypted = DecryptToBeParsed(encrypted);
            jsonNode = JsonNode.Parse(decrypted)?.AsObject();
        }
        
        var sourceArray = jsonNode?["episode"]?["sourceUrls"];
        var sourceObjs = sourceArray?.Deserialize<List<SourceUrlObj>>() ?? [];
        sourceObjs.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        foreach (var item in sourceObjs)
        {
            ct.ThrowIfCancellationRequested();
            
            item.SourceUrl = DecryptSourceUrl(item.SourceUrl);

            switch (item.SourceName)
            {
                case "Mp4":
                    if (await VideoServers.FromMp4Upload(item.SourceName, item.SourceUrl) is { } server)
                    {
                        yield return server;
                    }

                    continue;
                case "Yt-mp4":
                    yield return VideoServers.WithReferer(item.SourceName, item.SourceUrl, "https://allanime.day/")
                                             .WithContentType("mp4");
                    continue;
                case "Vg":
                case "Fm-Hls":
                case "Sw":
                case "Ok":
                case "Ss-Hls":
                case "Vid-mp4":
                    continue;
            }

            JsonObject? jObject;
            try
            {
                var response = await $"https://allanime.day{item.SourceUrl.Replace("clock", "clock.json")}".GetStringAsync(cancellationToken:ct);
                jObject = JsonNode.Parse(response)!.AsObject();
            }
            catch
            {
                continue;
            }

            switch (item.SourceName)
            {
                case "Luf-Mp4" or "S-mp4":
                    var links = jObject["links"].Deserialize<List<ApiV2Response>>() ?? [];
                    if (!string.IsNullOrEmpty(links[0].Url))
                    {
                        yield return VideoServers.WithReferer(item.SourceName, links[0].Url, "https://allanime.day/");
                    }

                    continue;
                case "Default":
                    var hls = jObject["links"].Deserialize<List<DefaultResponse>>() ?? [];
                    yield return new VideoServer(item.SourceName, new Uri(hls[0].Link));
                    continue;
            }
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
    
    private static string DecryptSourceUrl(string input)
    {
        string hexPayload;
        int keyType;

        if (input.StartsWith("--"))
        {
            hexPayload = input[2..];
            keyType = 3;
        }
        else if (input.StartsWith("#-"))
        {
            hexPayload = input[2..];
            keyType = 2;
        }
        else if (input.StartsWith("##"))
        {
            hexPayload = input[2..];
            keyType = 1;
        }
        else if (input.StartsWith("-#"))
        {
            hexPayload = input[2..];
            keyType = 4;
        }
        else if (input.StartsWith('#'))
        {
            hexPayload = input[1..];
            keyType = 0;
        }
        else
        {
            return input;
        }

        var mask = XorMasks[keyType];

        // Process hex string two characters at a time
        var result = string.Concat(
                                   Enumerable.Range(0, hexPayload.Length / 2)
                                             .Select(i =>
                                             {
                                                 var hexByte = hexPayload.Substring(i * 2, 2);
                                                 var value = Convert.ToInt32(hexByte, 16);
                                                 var decoded = (value ^ mask) & 0xFF;
                                                 return (char)decoded;
                                             })
                                  );

        return result;
    }

    private static List<string> GetEpisodeDetails(EpisodeDetails? details, TranslationType type)
    {
        if (details is null)
        {
            return [];
        }

        return type switch
        {
            TranslationType.Dub => details.Dub,
            TranslationType.Raw => details.Raw,
            _ => details.Sub
        };
    }

    private static string GetTranslationType(TranslationType type)
    {
        return type switch
        {
            TranslationType.Raw => "raw",
            TranslationType.Dub => "dub",
            _ => "sub"
        };
    }
    
    public static string DecryptToBeParsed(string base64Payload)
    {
        // 1. Decode the Base64 payload
        var blob = Convert.FromBase64String(base64Payload);

        // 2. Extract version byte, IV, and ciphertext
        if (blob.Length < 13) return string.Empty;
        var versionByte = blob[0] & 0xFF;
        var iv = new byte[12];
        Array.Copy(blob, 1, iv, 0, 12);
        var encryptedData = new byte[blob.Length - 13];
        Array.Copy(blob, 13, encryptedData, 0, encryptedData.Length);

        // 3. Derive the AES-GCM key: SHA-256($"{DECRYPT_SECRET}:v{versionByte}")
        var keyMaterial = $"{DecryptSecret}:v{versionByte}";
        var keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));

        // 4. Initialize AES-GCM Cipher
        var plaintext = new byte[encryptedData.Length - (DecryptTagLength / 8)];
        var ciphertext = new byte[encryptedData.Length - (DecryptTagLength / 8)];
        var tag = new byte[DecryptTagLength / 8];

        // Split ciphertext and tag (last 16 bytes for 128-bit tag)
        Array.Copy(encryptedData, 0, ciphertext, 0, ciphertext.Length);
        Array.Copy(encryptedData, ciphertext.Length, tag, 0, tag.Length);

        using (var aesGcm = new AesGcm(keyBytes, tag.Length))
        {
            aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
        }

        // 5. Return JSON string
        return Encoding.UTF8.GetString(plaintext);
    }

}