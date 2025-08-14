using System.Diagnostics;
using System.Text.Json.Serialization;

namespace TotoroNext.Anime.AllAnime;

[Serializable]
internal sealed class EpisodeDetails
{
    [JsonPropertyName("sub")] public List<string> Sub { get; init; } = [];

    [JsonPropertyName("dub")] public List<string> Dub { get; init; } = [];

    [JsonPropertyName("raw")] public List<string> Raw { get; init; } = [];
}

[DebuggerDisplay("{Priority} - {SourceUrl} - {Type}")]
[Serializable]
internal sealed class SourceUrlObj
{
    [JsonPropertyName("sourceName")] public string SourceName { get; set; } = "";

    [JsonPropertyName("sourceUrl")] public string SourceUrl { get; set; } = "";

    [JsonPropertyName("priority")] public double Priority { get; set; }

    [JsonPropertyName("type")] public string Type { get; set; } = "";
}

[Serializable]
public sealed class ApiV2Response
{
    [JsonPropertyName("src")] public string Url { get; set; } = "";

    [JsonPropertyName("headers")] public Dictionary<string, string> Headers { get; set; } = [];
}

[Serializable]
public sealed class DefaultResponse
{
    [JsonPropertyName("link")] public string Link { get; set; } = string.Empty;

    [JsonPropertyName("hls")] public bool Hls { get; set; }

    [JsonPropertyName("resolutionStr")] public string ResolutionString { get; set; } = string.Empty;

    [JsonPropertyName("resolution")] public int Resolution { get; set; } = 0;

    [JsonPropertyName("src")] public string Src { get; set; } = string.Empty;
}