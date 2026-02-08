namespace TotoroNext.Anime.Abstractions;

[Serializable]
public sealed class AnimeId
{
    public long AniDb { get; set; }
    public long MyAnimeList { get; set; }
    public long Anilist { get; set; }
    public long Kitsu { get; set; }
    public long Simkl { get; set; }
    public long Local => MyAnimeList;

    public long? GetIdForService(string serviceType)
    {
        if (GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(serviceType, StringComparison.OrdinalIgnoreCase)) is not { } property)
        {
            return null;
        }

        return (long?)property.GetValue(this);
    }
    
    public static bool EqualsForAnimeRelations(AnimeId first, AnimeId second)
    {
        return first.MyAnimeList == second.MyAnimeList &&
               first.Anilist == second.Anilist &&
               first.Kitsu == second.Kitsu;
    }
}