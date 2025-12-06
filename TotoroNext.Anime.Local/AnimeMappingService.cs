using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.Local;

internal class AnimeMappingService(ILiteDbContext dbContext) : IAnimeMappingService
{
    public AnimeId? GetId(AnimeModel anime)
    {
        if (anime.ServiceName == "Local")
        {
            return anime.ExternalIds;
        }
        
        var localAnime = anime.ServiceName switch
        {
            "MyAnimeList" => dbContext.Anime.FindOne(x => x.MyAnimeListId == anime.Id),
            "Anilist" => dbContext.Anime.FindById(anime.Id),
            "AniDb" => dbContext.Anime.FindOne(x => x.AniDbId == anime.Id),
            "Kitsu" => dbContext.Anime.FindOne(x => x.KitsuId == anime.Id),
            "Simkl" => dbContext.Anime.FindOne(x => x.SimklId == anime.Id),
            _ => throw new ArgumentException("Invalid service name")
        };

        if (localAnime is null)
        {
            return null;
        }

        return new AnimeId
        {
            MyAnimeList = localAnime.MyAnimeListId,
            Anilist = localAnime.AnilistId,
            AniDb = localAnime.AniDbId,
            Kitsu = localAnime.KitsuId,
            Simkl = localAnime.SimklId,
        };
    }
}