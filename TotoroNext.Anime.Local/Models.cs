namespace TotoroNext.Anime.Local;

[Serializable]
internal class AnimeModelRemote
{
    public AnimeTitle Title { get; set; } = new();
    public int Id { get; set; }
    public ExternalId ExternalIds { get; set; } = new();
    public string Description { get; set; } = "";
    public string Format { get; set; } = "";
    public string Season { get; set; } = "";
    public int SeasonYear { get; set; }
    public int MeanScore { get; set; }
    public int Popularity { get; set; }
    public int? TotalEpisodes { get; set; }
    public string? CoverImage { get; set; } = "";
    public string? BannerImage { get; set; }
    public string Status { get; set; } = "FINISHED";
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string MainStudio { get; set; } = "";
    public List<string> SupportingStudios { get; set; } = [];
    public List<string> Genres { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public AnimeTrailer? Trailer { get; set; }
    public List<AnimeRelationship> Relations { get; set; } = [];
}

[Serializable]
internal class AnimeRelationship
{
    public string RelationType { get; set; } = "";
    public int Id { get; set; }
}

[Serializable]
internal class AnimeTitle
{
    public string? English { get; set; }
    public string? Native { get; set; } = "";
    public string? Romaji { get; set; } = "";
}

[Serializable]
internal class AnimeTrailer
{
    public string Url { get; set; } = "";
    public string? Thumbnail { get; set; }
}

[Serializable]
internal class ExternalId
{
    public int MyAnimeList { get; set; }
    public int? Kitsu { get; set; }
    public int? AnimeNewsNetwork { get; set; }
    public int? Anidb { get; set; }
    public int? Simkl { get; set; }
}