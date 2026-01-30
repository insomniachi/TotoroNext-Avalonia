namespace TotoroNext.Anime.Abstractions;

public class AnimeRelations : List<AnimeRelation>, IAnimeRelations
{
    public void AddRelation(AnimeRelation relation)
    {
        Add(relation);
    }

    public bool Exists(AnimeRelation relation)
    {
        return this.Any(x => AnimeRelation.AreEqual(x, relation));
    }

    public AnimeRelation? FindRelation(Models.AnimeModel anime)
    {
        var relation = this.FirstOrDefault(x => x.DestinationIds.GetIdForService(anime.ServiceName!) == anime.Id);
        return relation;
    }
}