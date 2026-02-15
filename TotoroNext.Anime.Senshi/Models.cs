using System.Text.Json.Serialization;

namespace TotoroNext.Anime.Senshi;

[Serializable]
public class SenshiItem
{
    [JsonPropertyName("id")] public required int InternalId { get; set; }
    [JsonPropertyName("public_id")] public required string Id { get; set; }

    [JsonPropertyName("anime_picture")] public required string Image { get; set; }

    [JsonPropertyName("title")] public required string Title { get; set; }
    
    [JsonPropertyName("anilist_id")] public long? AnilistId { get; set; }
}

[Serializable]
public class SenshiSearchResponse
{
    [JsonPropertyName("data")] public required List<SenshiItem> Items { get; set; } = [];
}

[Serializable]
public class SenshiEpisode
{
    [JsonPropertyName("ep_id")] public required float Episode { get; set; }
    [JsonPropertyName("ep_title")] public required string Title { get; set; }
}

[Serializable]
public class SenshiStream
{
    [JsonPropertyName("url")] public string StreamUrl { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Type { get; set; } = string.Empty;
}