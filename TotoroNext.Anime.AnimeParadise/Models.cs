using System.Text.Json.Serialization;

namespace TotoroNext.Anime.AnimeParadise;

[Serializable]
internal class SearchResponseRoot
{
    [JsonPropertyName("data")] public SearchResponse Data { get; set; } = new();
}

[Serializable]
internal class SearchResponse
{
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("searchData")] public List<AnimeParadiseModel> Items { get; init; } = [];
}

[Serializable]
internal class EpisodeResponse
{
    [JsonPropertyName("data")] public List<AnimeParadiseEpisode> Data { get; init; } = [];
}

[Serializable]
internal class AnimeParadiseModel
{
    [JsonPropertyName("_id")] public string Id { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("posterImage")] public ImageModel PosterImage { get; init; } = new();
}

[Serializable]
internal class ImageModel
{
    [JsonPropertyName("tiny")] public string Tiny { get; init; } = "";
    [JsonPropertyName("medium")] public string Medium { get; init; } = "";
    [JsonPropertyName("large")] public string Large { get; init; } = "";
    [JsonPropertyName("original")] public string Original { get; init; } = "";
}

[Serializable]
internal class AnimeParadiseEpisode
{
    [JsonPropertyName("uid")] public string Id { get; init; } = "";
    [JsonPropertyName("title")] public string Title { get; init; } = "";
    [JsonPropertyName("image")] public string PosterImage { get; init; } = "";
    [JsonPropertyName("number")] public string Number { get; set; } = "";
}

[Serializable]
internal class SkipDataItem
{
    [JsonPropertyName("start")] public int Start { get; set; }
    [JsonPropertyName("end")] public int End { get; set; }
}

[Serializable]
internal class SkipData
{
    [JsonPropertyName("intro")] public SkipDataItem Intro { get; set; } = new();
    [JsonPropertyName("outro")] public SkipDataItem Outro { get; set; } = new();
}