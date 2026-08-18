using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Anime.Miwayomi;

[Serializable]
internal class MiwayomiAnime
{
    [JsonPropertyName("url")] public required string Url { get; set; }
    [JsonPropertyName("title")] public required string Title { get; set; }
    [JsonPropertyName("thumbnail_url")] public string? ThumbnailUrl { get; set; }
}

[Serializable]
internal class MiwayomiEpisode
{
    [JsonPropertyName("url")] public required string Url { get; set; }
    [JsonPropertyName("episode_number")] public required float EpisodeNumber { get; set; }
}

[Serializable]
internal class MiwayomiProvider
{
    [JsonPropertyName("id")] public required string Id { get; set; }
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("lang")] public required string Language { get; set; }
    [JsonPropertyName("type")] public required string Type { get; set; }
    [JsonPropertyName("pkg")] public required string Package { get; set; }

    public override string ToString() => $"{Name} ({Language})";
}

[Serializable]
internal partial class MiwayomiProviderViewModel : ObservableObject
{
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("lang")] public required string Language { get; set; }
    [JsonPropertyName("pkg")] public required string Package { get; set; }
    [JsonPropertyName("version")]public required string Version { get; set; }
    [JsonPropertyName("apk")]public required string Apk { get; set; }
    [JsonPropertyName("sources")] public List<MiwayomiProviderSource> Sources { get; set; } = [];
    [JsonPropertyName("nsfw")]public bool IsNsfw { get; set; }
    
    [JsonPropertyName("installed")]
    [ObservableProperty]
    public partial bool IsInstalled { get; set; }
}

[Serializable]
internal class MiwayomiProviderSource
{
    [JsonPropertyName("name")] public required string Name { get; set; }
    [JsonPropertyName("lang")] public required string Language { get; set; }
}