using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Local;

[UsedImplicitly]
internal class AnimeMappingService(IDbContext dbContext) : IAnimeMappingService
{
    public async Task<AnimeId?> GetId(AnimeModel anime)
    {
        if (anime.ServiceName == "Local")
        {
            return anime.ExternalIds;
        }

        var localAnime = anime.ServiceName switch
        {
            "Anilist" => dbContext.Anime.FindById(anime.Id),
            "MyAnimeList" => dbContext.Anime.FindOne(x => x.MyAnimeListId == anime.Id),
            "AniDb" => dbContext.Anime.FindOne(x => x.AniDbId == anime.Id),
            "Kitsu" => dbContext.Anime.FindOne(x => x.KitsuId == anime.Id),
            "Simkl" => dbContext.Anime.FindOne(x => x.SimklId == anime.Id),
            "AnimeNewsNetwork" => dbContext.Anime.FindOne(x => x.AnnId == anime.Id),
            _ => throw new ArgumentException("Invalid service name")
        };

        if (localAnime is null)
        {
            return await anime.GetMappings();
        }

        return new AnimeId
        {
            MyAnimeList = localAnime.MyAnimeListId ?? 0,
            Anilist = localAnime.AnilistId,
            AniDb = localAnime.AniDbId ?? 0,
            Kitsu = localAnime.KitsuId ?? 0,
            Simkl = localAnime.SimklId ?? 0,
            AnimeNewsNetwork = localAnime.AnnId ?? 0
        };
    }
}