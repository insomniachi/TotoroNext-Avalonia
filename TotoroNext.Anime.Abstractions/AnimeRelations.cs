namespace TotoroNext.Anime.Abstractions;

public class AnimeRelations : List<AnimeRelation>, IAnimeRelations
{
    public void AddRelation(AnimeRelation relation)
    {
        Add(relation);
    }

    public AnimeRelation? FindRelation(AnimeModel anime)
    {
        return this.FirstOrDefault(x => x.DestinationIds.GetIdForService(anime.ServiceName!) == anime.Id);
    }
}