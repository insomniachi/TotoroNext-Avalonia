using System.Text.Json.Serialization;

namespace TotoroNext.Anime.AnimePahe;

internal class AnimePaheEpisodePage
{
    [JsonPropertyName("total")] public double Total { get; set; }

    [JsonPropertyName("per_page")] public int Perpage { get; set; }

    [JsonPropertyName("current_page")] public int CurrentPage { get; set; }

    [JsonPropertyName("last_page")] public int LastPage { get; set; }

    [JsonPropertyName("next_page_url")] public string NextPageUrl { get; set; } = string.Empty;

    [JsonPropertyName("prev_page_url")] public string PrevPageUrl { get; set; } = string.Empty;

    [JsonPropertyName("from")] public int From { get; set; }

    [JsonPropertyName("to")] public int To { get; set; }

    [JsonPropertyName("data")] public List<AnimePaheEpisodeInfo> Data { get; set; } = [];
}

internal class AnimePaheEpisodeInfo
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("anime_id")] public int AnimeId { get; set; }

    [JsonPropertyName("episode")] public double Episode { get; set; }

    [JsonPropertyName("episode2")] public double Episode2 { get; set; }

    [JsonPropertyName("edition")] public string Editon { get; set; } = string.Empty;

    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    [JsonPropertyName("snapshot")] public string Snapshot { get; set; } = string.Empty;

    [JsonPropertyName("disc")] public string Disc { get; set; } = string.Empty;

    [JsonPropertyName("duration")] public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("session")] public string Session { get; set; } = string.Empty;

    [JsonPropertyName("filler")] public int Filler { get; set; }

    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = string.Empty;
}

internal class AnimePaheEpisodeStream
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("filesize")] public int FileSize { get; set; }

    [JsonPropertyName("crc32")] public string Crc32 { get; set; } = string.Empty;

    [JsonPropertyName("revision")] public string Revision { get; set; } = string.Empty;

    [JsonPropertyName("fansub")] public string FanSub { get; set; } = string.Empty;

    [JsonPropertyName("audio")] public string Audio { get; set; } = string.Empty;

    [JsonPropertyName("disc")] public string Disc { get; set; } = string.Empty;

    [JsonPropertyName("hq")] public int HQ { get; set; }

    [JsonPropertyName("av1")] public int AV1 { get; set; }

    [JsonPropertyName("kwik")] public string Kwik { get; set; } = string.Empty;

    [JsonPropertyName("kwik_pahewin")] public string KwikPahewin { get; set; } = string.Empty;
}