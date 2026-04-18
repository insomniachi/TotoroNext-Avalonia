using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl.Http;
using FlurlGraphQL;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AllAnime;

internal class AnimeProvider(IModuleSettings<Settings> settings) : IAnimeProvider
{
    private const string DecryptSecret = "P7K2RGbFgauVtmiS";
    private const int DecryptIvLength = 12;   // typical IV length for AES-GCM
    private const int DecryptTagLength = 128; // bits
    
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
        var jsonNode = await GraphQl.Api
                                    .WithGraphQLQuery(GraphQl.EpisodeQuery)
                                    .SetGraphQLVariables(new
                                    {
                                        showId = animeId,
                                        translationType = GetTranslationType(settings.Value.TranslationType),
                                        episodeString = episodeId
                                    })
                                    .PostGraphQLQueryAsync(ct)
                                    .ReceiveGraphQLRawSystemTextJsonResponse();

        var encrypted = jsonNode?["tobeparsed"]?.GetValue<string>();
        if (encrypted is null)
        {
            yield break;
        }
        
        var decrypted = DecryptToBeParsed(encrypted);
        jsonNode = JsonNode.Parse(decrypted)?.AsObject();
        var sourceArray = jsonNode?["episode"]?["sourceUrls"];
        var sourceObjs = sourceArray?.Deserialize<List<SourceUrlObj>>() ?? [];
        sourceObjs.Sort((x, y) => y.Priority.CompareTo(x.Priority));

        foreach (var item in sourceObjs)
        {
            ct.ThrowIfCancellationRequested();
            
            if (item.SourceUrl.StartsWith("--"))
            {
                item.SourceUrl = DecryptSourceUrl(item.SourceUrl);
            }

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
                var response = await $"https://allanime.day{item.SourceUrl.Replace("clock", "clock.json")}".GetStringAsync();
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

    private static string Decrypt(string target)
    {
        return string.Join("", Convert.FromHexString(target).Select(x => (char)(x ^ 56)));
    }

    private static string DecryptSourceUrl(string sourceUrl)
    {
        var index = sourceUrl.LastIndexOf('-') + 1;
        var encrypted = sourceUrl[index..];
        return Decrypt(encrypted);
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
        var secretBytes = Encoding.UTF8.GetBytes(ReverseString(DecryptSecret));
        var keyBytes = SHA256.HashData(secretBytes);

        var decodedBytes = Convert.FromBase64String(base64Payload);

        var iv = new byte[DecryptIvLength];
        Array.Copy(decodedBytes, 0, iv, 0, DecryptIvLength);

        var encryptedData = new byte[decodedBytes.Length - DecryptIvLength];
        Array.Copy(decodedBytes, DecryptIvLength, encryptedData, 0, encryptedData.Length);

        var plaintext = new byte[encryptedData.Length - (DecryptTagLength / 8)];
        var tag = new byte[DecryptTagLength / 8];
        Array.Copy(encryptedData, encryptedData.Length - tag.Length, tag, 0, tag.Length);
        var ciphertext = new byte[encryptedData.Length - tag.Length];
        Array.Copy(encryptedData, 0, ciphertext, 0, ciphertext.Length);

        using (var aesGcm = new AesGcm(keyBytes, tag.Length))
        {
            aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
        }

        // 5. Convert back to a JSON string
        return Encoding.UTF8.GetString(plaintext);
    }

    private static string ReverseString(string s) => new(s.Reverse().ToArray());

}