using System.Text.Json.Serialization;

namespace TotoroNext.Anime.AnimeOnsen;

[Serializable]
internal class AnimeOnsenSearchResult
{
    [JsonPropertyName("hits")] public IReadOnlyList<AnimeOnsenItemModel> Data { get; init; } = [];
}

[Serializable]
internal class AnimeOnsenStreamResult
{
    [JsonPropertyName("uri")] public AnimeOnsenStream Stream { get; set; } = new();
}

[Serializable]
internal class AnimeOnsenItemModel
{
    [JsonPropertyName("content_title")] public string Title { get; set; } = "";
    [JsonPropertyName("content_id")] public string Id { get; set; } = "";
}

[Serializable]
internal class AnimeOnsenEpisode
{
    [JsonPropertyName("contentTitle_episode_en")]
    public string TitleEnglish { get; set; } = "";

    [JsonPropertyName("contentTitle_episode_jp")]
    public string TitleJapanese { get; set; } = "";
}

[Serializable]
internal class AnimeOnsenStream
{
    [JsonPropertyName("stream")] public string Url { get; init; } = "";
    [JsonPropertyName("subtitles")] public AnimeOnsenSubtitles Subtitles { get; init; } = new();
}

[Serializable]
internal class AnimeOnsenSubtitles
{
    [JsonPropertyName("en-US")] public string English { get; set; } = "";
}

[Serializable]
internal class AnimeOnsenSkipData
{
    [JsonPropertyName("skipIntro_s")] public string OpeningStart { get; init; } = "";
    [JsonPropertyName("skipIntro_e")] public string OpeningEnd { get; init; } = "";
    [JsonPropertyName("skipOutro_s")] public string EndingStart { get; init; } = "";
    [JsonPropertyName("skipOutro_e")] public string EndingEnd { get; init; } = "";
}