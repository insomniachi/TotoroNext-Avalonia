using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.AnimePahe;

public partial class KwikExtractor(IHttpClientFactory httpClientFactory) : IVideoExtractor
{
    internal const string ClientName = "RedirectOff";
    private const string CharacterMap = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";

    public async IAsyncEnumerable<VideoSource> Extract(Uri url, [EnumeratorCancellation] CancellationToken ct)
    {
        var httpClient = httpClientFactory.CreateClient(ClientName);
        var client = new FlurlClient(httpClient);
        var response = await client.Request(url).GetStringAsync(cancellationToken: ct);
        var redirectUrl = KwikRedirectionRegex().Match(response).Groups[1].Value;
        var downloadPage = await client.Request(redirectUrl).GetStringAsync(cancellationToken: ct);
        var match = KwikParamsRegex().Match(downloadPage);

        if (!match.Success)
        {
            yield break;
        }

        var fullKey = match.Groups[1].Value;
        var key = match.Groups[2].Value;
        var v1 = match.Groups[3].Value;
        var v2 = match.Groups[4].Value;

        var decrypted = Decrypt(fullKey, key, int.Parse(v1), int.Parse(v2));

        var postUrl = KwikDecryptUrlRegex().Match(decrypted).Groups[1].Value;
        var token = KwikDecryptTokenRegex().Match(decrypted).Groups[1].Value;

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_token"] = token
        });

        var httpResponse = await client.HttpClient.PostAsync(postUrl, content, ct);
        if (httpResponse.StatusCode == HttpStatusCode.Found)
        {
            yield return new VideoSource { Url = new Uri(httpResponse.Headers.Location!.AbsoluteUri) };
        }
    }

    private static string Decrypt(string fullString, string key, int v1, int v2)
    {
        var r = "";
        var i = 0;
        while (i < fullString.Length)
        {
            var s = "";
            while (fullString[i] != key[v2])
            {
                s += fullString[i];
                i++;
            }

            var j = 0;
            while (j < key.Length)
            {
                s = s.Replace(key[j].ToString(), j.ToString());
                j++;
            }

            r += (char)(int.Parse(GetString(s, v2, 10)) - v1);
            i++;
        }

        return r;
    }

    private static string GetString(string content, int s1, int s2)
    {
        var slice = CharacterMap.AsSpan()[..s2];
        var acc = 0;
        var index = 0;
        foreach (var item in content.Reverse())
        {
            acc += (char.IsDigit(item) ? int.Parse(item.ToString()) : 0) * (int)Math.Pow(s1, index);
            index++;
        }

        var k = "";
        while (acc > 0)
        {
            k = slice[acc % s2] + k;
            acc = (acc - acc % s2) / s2;
        }

        return string.IsNullOrEmpty(k) ? "0" : k;
    }


    [GeneratedRegex(@"\(""href"",\s*""(https://[^()]+)""\)")]
    private static partial Regex KwikRedirectionRegex();

    [GeneratedRegex(@"\(""(\w+)"",\d+,""(\w+)"",(\d+),(\d+),\d+\)")]
    private static partial Regex KwikParamsRegex();

    [GeneratedRegex(@"action=""(.+?)""")]
    private static partial Regex KwikDecryptUrlRegex();

    [GeneratedRegex(@"value=""(.+?)""")]
    private static partial Regex KwikDecryptTokenRegex();
}