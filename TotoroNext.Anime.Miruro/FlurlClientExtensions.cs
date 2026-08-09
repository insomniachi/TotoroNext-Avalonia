using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Flurl.Http;
using Microsoft.AspNetCore.WebUtilities;
using TotoroNext.Module;

namespace TotoroNext.Anime.Miruro;

public static class FlurlClientExtensions
{
    private static readonly byte[] PipeKey = Convert.FromHexString("71951034f8fbcf53d89db52ceb3dc22c");
    
    extension(IFlurlClient client)
    {
        internal async ValueTask<string> SendPipeAsync(string path, JsonObject query, CancellationToken cancellationToken)
        {
            var payload = new JsonObject
            {
                ["path"] = path,
                ["method"] = "GET",
                ["query"] = query,
                ["body"] = null,
                ["version"] = "0.2.0"
            };
        
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(payload.ToJsonString()));
        
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{AnimeProvider.BaseUrl}/api/secure/pipe?e={encoded}");
            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            foreach (var (key, value) in ApiFingerprintHeaders(AnimeProvider.BaseUrl))
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }

            using var response = await client.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var obfuscated = response.Headers.TryGetValues("x-obfuscated", out var values) 
                ? values.FirstOrDefault() 
                : "1";

            return obfuscated == "2" ? DecryptPipeResponse(body) : body.Trim();
        }
    }
    
    private static string DecryptPipeResponse(string body)
    {
        var decoded = WebEncoders.Base64UrlDecode(body.Trim());
        for (var i = 0; i < decoded.Length; i++)
        {
            decoded[i] = (byte)(decoded[i] ^ PipeKey[i % PipeKey.Length]);
        }

        using var input = new MemoryStream(decoded);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static Dictionary<string, string> ApiFingerprintHeaders(string origin, string? referer = null, bool sameOrigin = true)
    {
        const string chromeMajorVersion = "148";
        return new Dictionary<string, string>
        {
            ["Accept"] = "*/*",
            ["Accept-Language"] = "en-US,en;q=0.9",
            ["User-Agent"] = Http.UserAgent,
            ["Sec-Ch-Ua"] = $"\"Chromium\";v=\"{chromeMajorVersion}\", \"Not_A Brand\";v=\"24\", \"Google Chrome\";v=\"{chromeMajorVersion}\"",
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