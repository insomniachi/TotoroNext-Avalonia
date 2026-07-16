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
        if (this.FirstOrDefault(x => x.DestinationIds.GetIdForService(anime.ServiceName!) == anime.Id) is { } relation)
        {
            return relation;
        }

        var secondSeason = this.Where(x => x.SourceIds.GetIdForService(anime.ServiceName!) == anime.Id)
                               .MinBy(x => x.SourceEpisodesRage.Start);

        if (secondSeason is null)
        {
            return null;
        }

        return new AnimeRelation()
        {
            SourceIds = secondSeason.SourceIds,
            DestinationIds = secondSeason.SourceIds,
            SourceEpisodesRage = new EpisodeRange(1, secondSeason.SourceEpisodesRage.Start - 1),
            DestinationEpisodesRage = new EpisodeRange(1, secondSeason.SourceEpisodesRage.Start - 1)
        };
    }
}