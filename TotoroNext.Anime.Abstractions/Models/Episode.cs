using System.Text.Json.Serialization;

namespace TotoroNext.Anime.Abstractions.Models;

public class Episode(IAnimeProvider provider, string showId, string id, float number, string name = "", Uri? image = null)
{
    private readonly IAnimeProvider _provider = provider;

    public string ShowId { get; } = showId;
    public string Id { get; } = id;
    public float Number { get; } = number;
    public string Name { get; } = name;
    public Uri? Image { get; } = image;
    public TimeSpan StartPosition { get; set; } = TimeSpan.Zero;
    public bool IsCompleted { get; set; }

    public IAsyncEnumerable<VideoServer> GetServersAsync() => _provider.GetServersAsync(ShowId, Id);
}

public class EpisodeInfo
{
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("absoluteEpisodeNumber")]
    public int AbsoluteEpisodeNumber { get; set; }

    [JsonPropertyName("title")]
    public Titles Titles { get; set; } = new();

    [JsonPropertyName("overview")]
    public string Overview { get; set; } = "";

    [JsonPropertyName("image")]
    public string Image { get; set; } = "";

    [JsonPropertyName("airDate")]
    public string AirDate { get; set; } = "";

    [JsonPropertyName("runtime")]
    public int Runtime { get; set; }

    [JsonPropertyName("airDateUtc")]
    public DateTime? AirDateUtc { get; set; }

    [JsonIgnore]
    public ProgressInfo? Progress { get; set; }
}

public class Titles
{
    [JsonPropertyName("ja")]
    public string Japanese { get; set; } = "";

    [JsonPropertyName("en")]
    public string English { get; set; } = "";

    [JsonPropertyName("x-jat")]
    public string Romaji { get; set; } = "";
}
