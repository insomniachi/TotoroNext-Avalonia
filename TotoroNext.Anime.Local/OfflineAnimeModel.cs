using System.Text.Json.Serialization;
using LiteDB;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Local;

[Serializable]
internal class LocalAnimeModel
{
    [BsonRef(nameof(LocalTracking))] public LocalTracking? Tracking { get; set; }
    [BsonRef(nameof(LocalEpisodeInfo))] public LocalEpisodeInfo? EpisodeInfo { get; set; }
    [BsonRef(nameof(LocalCharacterInfo))] public LocalCharacterInfo? CharacterInfo { get; set; }
    [BsonRef(nameof(LocalAdditionalInfo))] public LocalAdditionalInfo? AdditionalInfo { get; set; }
    [BsonId] public long MyAnimeListId { get; set; }
    public long AnilistId { get; set; }
    public long KitsuId { get; set; }
    public long AniDbId { get; set; }
    public long SimklId { get; set; }
    public string Title { get; set; } = "";
    public int TotalEpisodes { get; set; }
    public IReadOnlyCollection<string> Genres { get; set; } = [];
    public Season? Season { get; set; }
    public float MeanScore { get; set; }
    public IReadOnlyCollection<string> Studios { get; set; } = [];
    public AiringStatus AiringStatus { get; set; }
    public AnimeMediaFormat MediaFormat { get; set; }
    public IReadOnlyCollection<long> Related { get; set; } = [];
    public string Image { get; set; } = "";
    public string Thumbnail { get; set; } = "";

    public bool HasChanged(LocalAnimeModel other)
    {
        return TotalEpisodes != other.TotalEpisodes ||
               Math.Abs(MeanScore - other.MeanScore) > 0 ||
               AiringStatus != other.AiringStatus;
    }
}

[Serializable]
internal class LocalTracking
{
    [BsonId] public long Id { get; set; }
    public Tracking Tracking { get; set; } = new();
}

[Serializable]
internal class LocalEpisodeInfo
{
    [BsonId] public long Id { get; set; }
    public List<EpisodeInfo> Info { get; set; } = [];
    public DateTimeOffset ExpiresAt { get; set; }
}

[Serializable]
internal class LocalCharacterInfo
{
    [BsonId] public long Id { get; set; }
    public List<CharacterModel> Characters { get; set; } = [];
    public DateTimeOffset ExpiresAt { get; set; }
}

[Serializable]
internal class LocalAdditionalInfo
{
    [BsonId] public long Id { get; set; }
    public OfflineAdditionalInfo Info { get; set; } = new();
    public DateTimeOffset ExpiresAt { get; set; }
}

[Serializable]
internal class OfflineAdditionalInfo
{
    public string TitleEnglish { get; set; } = "";
    public string TitleRomaji { get; set; } = "";
    public string Description { get; set; } = "";
    public int Popularity { get; set; }
    public List<TrailerVideo> Videos { get; set; } = [];
    public string BannerImage { get; set; } = "";
}

[Serializable]
internal class OfflineDbAnimeSeason
{
    [JsonPropertyName("season")] public string Season { get; set; } = "";

    [JsonPropertyName("year")] public int Year { get; set; }
}

[Serializable]
internal class Duration
{
    [JsonPropertyName("value")] public int Value { get; set; }

    [JsonPropertyName("unit")] public string Unit { get; set; } = "";
}

[Serializable]
internal class Score
{
    [JsonPropertyName("arithmeticGeometricMean")]
    public double ArithmeticGeometricMean { get; set; }

    [JsonPropertyName("arithmeticMean")] public double ArithmeticMean { get; set; }

    [JsonPropertyName("median")] public double Median { get; set; }
}

[Serializable]
internal class Anime
{
    [JsonPropertyName("sources")] public List<string> Sources { get; set; } = [];

    [JsonPropertyName("title")] public string Title { get; set; } = "";

    [JsonPropertyName("type")] public string Type { get; set; } = "";

    [JsonPropertyName("episodes")] public int Episodes { get; set; }

    [JsonPropertyName("status")] public string Status { get; set; } = "";

    [JsonPropertyName("animeSeason")] public OfflineDbAnimeSeason? AnimeSeason { get; set; }

    [JsonPropertyName("picture")] public string Picture { get; set; } = "";

    [JsonPropertyName("thumbnail")] public string Thumbnail { get; set; } = "";

    [JsonPropertyName("duration")] public Duration? Duration { get; set; }

    [JsonPropertyName("score")] public Score? Score { get; set; }

    [JsonPropertyName("synonyms")] public List<string> Synonyms { get; set; } = [];

    [JsonPropertyName("studios")] public List<string> Studios { get; set; } = [];

    [JsonPropertyName("producers")] public List<string> Producers { get; set; } = [];

    [JsonPropertyName("relatedAnime")] public List<string> RelatedAnime { get; set; } = [];

    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = [];
}