using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Anime.Abstractions.Models;

public partial class Episode(IAnimeProvider provider, string showId, string id, float number) : ObservableObject
{
    public string ShowId { get; } = showId;
    public string Id { get; } = id;
    public float Number { get; set; } = number;
    [ObservableProperty] public partial EpisodeInfo? Info { get; set; } = new();
    public TimeSpan StartPosition { get; set; } = TimeSpan.Zero;
    public bool IsCompleted { get; set; }

    public IAsyncEnumerable<VideoServer> GetServersAsync()
    {
        return provider.GetServersAsync(ShowId, Id);
    }

    public bool IsFromProvider(Type providerType)
    {
        return provider.GetType() == providerType;
    }
}

[Serializable]
public class EpisodeInfo
{
    [JsonPropertyName("seasonNumber")] public int SeasonNumber { get; set; }

    [JsonPropertyName("episodeNumber")] public int EpisodeNumber { get; set; }

    [JsonPropertyName("absoluteEpisodeNumber")]
    public int AbsoluteEpisodeNumber { get; set; }

    [JsonPropertyName("title")] public Titles Titles { get; set; } = new();

    [JsonPropertyName("overview")] public string Overview { get; set; } = "";

    [JsonPropertyName("image")] public string Image { get; set; } = "";

    [JsonPropertyName("airDate")] public string AirDate { get; set; } = "";

    [JsonPropertyName("runtime")] public int Runtime { get; set; }

    [JsonPropertyName("airDateUtc")] public DateTime? AirDateUtc { get; set; }

    [JsonIgnore] public ProgressInfo? Progress { get; set; }

    [JsonIgnore] public bool IsSpecial { get; set; }
}

[Serializable]
public class Titles
{
    [JsonPropertyName("ja")] public string Japanese { get; set; } = "";

    [JsonPropertyName("en")] public string English { get; set; } = "";

    [JsonPropertyName("x-jat")] public string Romaji { get; set; } = "";
}