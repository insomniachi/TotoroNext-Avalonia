using System.Text.Json.Serialization;

namespace TotoroNext.Anime.Anikoto;

[Serializable]
public class StreamResponse
{
    [JsonPropertyName("result")] public StreamResult Result { get; set; } = new();
}

[Serializable]
public class StreamResult
{
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("skip_data")] public SkipData SkipData { get; set; } = new();
}

[Serializable]
public class SkipData
{
    [JsonPropertyName("intro")] public int[] Intro { get; set; } = [];
    [JsonPropertyName("outro")] public int[] Outro { get; set; } = [];
}

[Serializable]
public class PlayerResponse
{
    [JsonPropertyName("sources")] public Sources Sources { get; set; } = new();

    [JsonPropertyName("tracks")] public List<Track> Tracks { get; set; } = [];

    [JsonPropertyName("t")] public int T { get; set; }

    [JsonPropertyName("intro")] public Segment Intro { get; set; } = new();

    [JsonPropertyName("outro")] public Segment Outro { get; set; } = new();

    [JsonPropertyName("server")] public int Server { get; set; }
}

[Serializable]
public class Sources
{
    [JsonPropertyName("file")] public string File { get; set; } = "";
}

[Serializable]
public class Track
{
    [JsonPropertyName("file")] public string File { get; set; } = "";

    [JsonPropertyName("label")] public string Label { get; set; } = "";

    [JsonPropertyName("kind")] public string Kind { get; set; } = "";

    [JsonPropertyName("default")] public bool Default { get; set; }
}

[Serializable]
public class Segment
{
    [JsonPropertyName("start")] public int Start { get; set; }
    [JsonPropertyName("end")] public int End { get; set; }
}