using System.Net.Http.Headers;
using System.Text.Json;
using TotoroNext.SongRecognition.Models;

namespace TotoroNext.SongRecognition;

internal static class ShazamApi
{
    private const string Country = "US";
    private static readonly HttpClient Http = new();
    private static readonly string InstallationId = Guid.NewGuid().ToString();

    static ShazamApi()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7");
    }

    public static async Task<ShazamResult> SendRequestAsync(string tagId, int samplems, byte[] sig)
    {
        using var payloadStream = new MemoryStream();
        await using var payloadWriter = new Utf8JsonWriter(payloadStream);

        payloadWriter.WriteStartObject();
        payloadWriter.WritePropertyName("signatures");
        payloadWriter.WriteStartArray();
        payloadWriter.WriteStartObject();
        payloadWriter.WriteString("uri", "data:audio/vnd.shazam.sig;base64," + Convert.ToBase64String(sig));
        payloadWriter.WriteNumber("samplems", samplems);
        payloadWriter.WriteEndObject();
        payloadWriter.WriteEndArray();
        payloadWriter.WriteString("timezone", "GMT");
        payloadWriter.WriteEndObject();
        await payloadWriter.FlushAsync();

        var url = "https://amp.shazam.com/match/v1/en/" + Country + "/android/" + InstallationId + "/" + tagId;
        var postData = new ByteArrayContent(payloadStream.GetBuffer(), 0, (int)payloadStream.Length);
        postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var result = new ShazamResult();

        var res = await Http.PostAsync(url, postData);
        var json = await res.Content.ReadAsByteArrayAsync();
        var obj = ParseJson(json);

        PopulateResult(obj, result);

        return result;
    }

    private static JsonElement ParseJson(byte[] json)
    {
        var reader = new Utf8JsonReader(json.AsSpan());
        return JsonElement.ParseValue(ref reader);
    }

    private static void PopulateResult(JsonElement rootElement, ShazamResult result)
    {
        if (!rootElement.TryGetProperty("results", out var resultsElement))
        {
            return;
        }

        PopulateId(resultsElement, result);

        if (!string.IsNullOrEmpty(result.ID))
        {
            result.Success = true;
            PopulateAttributes(rootElement, result);
        }
        else
        {
            PopulateRetryMs(resultsElement, result);
        }
    }

    private static void PopulateId(JsonElement resultsElement, ShazamResult result)
    {
        if (!resultsElement.TryGetProperty("matches", out var matchesElement))
        {
            return;
        }

        TryGetFirstItemId(matchesElement, out result.ID);
    }

    private static void PopulateRetryMs(JsonElement resultsElement, ShazamResult result)
    {
        if (!TryGetNestedProperty(resultsElement, ["retry", "retryInMilliseconds"], out var retryMsElement))
        {
            return;
        }

        if (!retryMsElement.TryGetInt32(out var retryMs))
        {
            return;
        }

        result.RetryMs = retryMs;
    }

    private static void PopulateAttributes(JsonElement rootElement, ShazamResult result)
    {
        if (!TryGetNestedProperty(rootElement, ["resources", "shazam-songs", result.ID], out var shazamSongElement))
        {
            return;
        }

        if (!shazamSongElement.TryGetProperty("attributes", out var attrsElement))
        {
            return;
        }

        if (attrsElement.TryGetProperty("title", out var titleElement))
        {
            result.Title = titleElement.GetString();
        }

        if (attrsElement.TryGetProperty("artist", out var artistElement))
        {
            result.Artist = artistElement.GetString();
        }

        if (attrsElement.TryGetProperty("webUrl", out var webUrlElement))
        {
            result.Url = webUrlElement.GetString();
        }

        if (!string.IsNullOrEmpty(result.Url))
        {
            result.Url = ImproveUrl(result.Url);
        }
        else
        {
            result.Url = "https://www.shazam.com/track/" + result.ID;
        }

        PopulateAppleId(shazamSongElement, result);

        if (string.IsNullOrEmpty(result.AppleSongID))
        {
            // As of March 2024
            // shazam.com/track/<ID> redirects to shazam.com/song/<AppleSongID>
            if (TryGetNestedProperty(attrsElement, ["share", "html"], out var shareHtmlElement))
            {
                result.Url = shareHtmlElement.GetString();
            }
        }
        else
        {
            // Some URLs redirect to / unless the 'co' parameter is kept
            // Examples: 11180294, 51774667, 538859473
            result.Url = result.Url + "?co=" + Country;
        }
    }

    private static string? ImproveUrl(string? url)
    {
        if (url is null)
        {
            return url;
        }

        var qsIndex = url.IndexOf('?');
        if (qsIndex > -1)
        {
            url = url[..qsIndex];
        }

        // make slug readable
        url = Uri.UnescapeDataString(url);

        return url;
    }

    private static void PopulateAppleId(JsonElement shazamSongElement, ShazamResult result)
    {
        if (!shazamSongElement.TryGetProperty("relationships", out var relationshipsElement))
        {
            return;
        }

        if (TryGetNestedProperty(relationshipsElement, ["songs", "data"], out var songsElement))
        {
            TryGetFirstItemId(songsElement, out result.AppleSongID);
        }
    }

    private static bool TryGetFirstItemId(JsonElement array, out string? id)
    {
        if (array.ValueKind == JsonValueKind.Array && array.GetArrayLength() > 0)
        {
            if (array[0].TryGetProperty("id", out var itemElement))
            {
                id = itemElement.GetString();
                return true;
            }
        }

        id = null;
        return false;
    }

    private static bool TryGetNestedProperty(JsonElement element, string?[] names, out JsonElement value)
    {
        if (names.OfType<string>().Any(name => !element.TryGetProperty(name, out element)))
        {
            value = default;
            return false;
        }

        value = element;
        return true;
    }
}