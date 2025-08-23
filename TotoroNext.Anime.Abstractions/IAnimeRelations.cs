namespace TotoroNext.Anime.Abstractions;

public interface IAnimeRelations : IReadOnlyList<AnimeRelation>
{
    void AddRelation(AnimeRelation relation);

    AnimeRelation? FindRelation(AnimeModel anime);
}