using LiteDB;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Local;

internal class OfflineAnimeModel
{
    [BsonRef(nameof(LocalTracking))] public LocalTracking? Tracking { get; set; }
    [BsonId] public long AnilistId { get; set; }
    public long? MyAnimeListId { get; set; }
    public long? KitsuId { get; set; }
    public long? AniDbId { get; set; }
    public long? SimklId { get; set; }
    public long? AnnId { get; set; }
    public AnimeTitle Title { get; set; } = new();
    public int TotalEpisodes { get; set; }
    public Season? Season { get; set; }
    public float MeanScore { get; set; }
    public string Image { get; set; } = "";
    public string Thumbnail { get; set; } = "";
    public string Description { get; set; } = "";
    public IReadOnlyCollection<string> Genres { get; set; } = [];
    public IReadOnlyCollection<string> Studios { get; set; } = [];
    public AiringStatus AiringStatus { get; set; } = AiringStatus.FinishedAiring;
    public AnimeMediaFormat MediaFormat { get; set; }
    public IReadOnlyCollection<AnimeRelationship> Related { get; set; } = [];
    public AnimeTrailer? Trailer { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

[Serializable]
internal class LocalTracking
{
    [BsonId] public long Id { get; set; }
    public Tracking Tracking { get; set; } = new();
}