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
    [JsonPropertyName("content_title_en")] public string TitleEnglish { get; set; } = string.Empty;
    [JsonPropertyName("content_title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("content_id")] public string Id { get; set; } = string.Empty;
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
internal class ResultResponse<T>
{
    [JsonPropertyName("status")] public int Status { get; set; }
    [JsonPropertyName("result")] public T? Result { get; set; }
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
    [JsonPropertyName("de-DE")] public string German { get; set; } = "";
    [JsonPropertyName("es-LA")] public string Spanish { get; set; } = "";
    [JsonPropertyName("fr-FR")] public string French { get; set; } = "";
    [JsonPropertyName("it-IT")] public string Italian { get; set; } = "";
    [JsonPropertyName("pt-BR")] public string PortugueseBrazil { get; set; } = "";

    public string Get(string language)
    {
        return language switch
        {
            "en-US" => English,
            "de-DE" => German,
            "es-LA" => Spanish,
            "fr-FR" => French,
            "it-IT" => Italian,
            "pt-BR" => PortugueseBrazil,
            _ => English
        };
    }
}

[Serializable]
internal class AnimeOnsenSkipData
{
    [JsonPropertyName("skipIntro_s")] public string OpeningStart { get; init; } = "";
    [JsonPropertyName("skipIntro_e")] public string OpeningEnd { get; init; } = "";
    [JsonPropertyName("skipOutro_s")] public string EndingStart { get; init; } = "";
    [JsonPropertyName("skipOutro_e")] public string EndingEnd { get; init; } = "";
}