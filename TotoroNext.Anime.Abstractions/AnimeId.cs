namespace TotoroNext.Anime.Abstractions;

public class AnimeId
{
    public long AniDb { get; set; }
    public long MyAnimeList { get; set; }
    public long Anilist { get; set; }
    public long Kitsu { get; set; }
    public long Simkl { get; set; }
    public string NotifyMoe { get; set; } = "";

    public long? GetIdForService(string serviceType)
    {
        if (GetType().GetProperties().FirstOrDefault(x => x.Name.Equals(serviceType, StringComparison.OrdinalIgnoreCase)) is not { } property)
        {
            return null;
        }

        return (long?)property.GetValue(this);
    }
}