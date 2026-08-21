using FuzzySharp;
using GraphQL.Client.Http;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Local;

[UsedImplicitly]
internal class MetadataService(
    IDbContext dbContext,
    IAnimeMappingService mappingService,
    GraphQLHttpClient client) : ILocalMetadataService
{
    public Guid Id => Module.Descriptor.Id;

    public string Name => "Local";

    public Task<AnimeModel?> GetAnimeWithoutAdditionalInfoAsync(long id)
    {
        return Task.Run(() =>
        {
            var anime = dbContext.Anime.FindById(id);
            return anime is null ? null : Converter.ToAppModel(anime);
        });
    }

    public Task<AnimeModel> GetAnimeAsync(long id)
    {
        return Task.Run(async () =>
        {
            var anime = dbContext.Anime.FindById(id);

            if (anime.Tracking?.Tracking.WatchedEpisodes == anime.TotalEpisodes &&
                anime is { AiringStatus: AiringStatus.CurrentlyAiring, AnilistId: > 0 })
            {
                var totalEpisodes = await AnilistHelper.GetTotalAiredEpisodes(client, anime.AnilistId, CancellationToken.None);
                if (totalEpisodes > 0)
                {
                    anime.TotalEpisodes = totalEpisodes;
                }
            }

            return Converter.ToAppModel(anime);
        });
    }

    public Task<List<AnimeModel>> SearchAnimeAsync(string term)
    {
        return Task.Run(() =>
        {
            var results = dbContext.Anime.FindAll().Select(x =>
                                   {
                                       var titleScore = Fuzz.TokenSetRatio(term, x.Title.Romaji);
                                       var alt = new[] { x.Title.Romaji, x.Title.English };
                                       var altScore = alt.Where(y => y is not null)
                                                         .Select(y => y!)
                                                         .Max(t => Fuzz.TokenSetRatio(term, t.ToLower()));
                                       var bestScore = Math.Max(titleScore, altScore);
                                       return (Anime: x, Score: bestScore);
                                   })
                                   .Where(x => x.Score >= 85)
                                   .OrderByDescending(x => x.Score)
                                   .Select(x => x.Anime)
                                   .Take(15)
                                   .Select(Converter.ToAppModel)
                                   .ToList();
            return results;
        });
    }

    public Task<List<AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request)
    {
        if (request.IsEmpty())
        {
            return Task.FromResult<List<AnimeModel>>([]);
        }

        return Task.Run(() =>
        {
            var candidates = dbContext.Anime.FindAll();
            var term = request.Title?.ToLower();
            if (!string.IsNullOrEmpty(term))
            {
                candidates = candidates.Select(x =>
                                       {
                                           var titleScore = Fuzz.TokenSetRatio(term, x.Title.Romaji);
                                           var alt = new[] { x.Title.Romaji, x.Title.English };
                                           var altScore = alt.Where(y => y is not null)
                                                             .Select(y => y!)
                                                             .Max(t => Fuzz.TokenSetRatio(term, t.ToLower()));
                                           var bestScore = Math.Max(titleScore, altScore);
                                           return (Anime: x, Score: bestScore);
                                       })
                                       .Where(x => x.Score >= 85)
                                       .OrderByDescending(x => x.Score)
                                       .Select(x => x.Anime);
            }

            if (request.MinYear.HasValue)
            {
                candidates = candidates.Where(x => x.Season?.Year >= request.MinYear.Value);
            }

            if (request.MaxYear.HasValue)
            {
                candidates = candidates.Where(x => x.Season?.Year <= request.MaxYear.Value);
            }

            if (request.SeasonName is { } season)
            {
                candidates = candidates.Where(x => x.Season?.SeasonName == season);
            }

            if (request.MinimumScore.HasValue)
            {
                candidates = candidates.Where(x => x.MeanScore >= request.MinimumScore.Value);
            }

            if (request.MaximumScore.HasValue)
            {
                candidates = candidates.Where(x => x.MeanScore <= request.MaximumScore.Value);
            }

            if (request.IncludedGenres is { Count: > 0 })
            {
                candidates = candidates.Where(x => request.IncludedGenres.All(tag => x.Genres.Contains(tag)));
            }

            if (request.ExcludedGenres is { Count: > 0 })
            {
                candidates = candidates.Where(x => request.ExcludedGenres.All(tag => !x.Genres.Contains(tag)));
            }

            var response = candidates.OrderByDescending(x => x.MeanScore)
                                     .Take(100)
                                     .Select(Converter.ToAppModel)
                                     .ToList();

            return response;
        });
    }

    public async Task<List<EpisodeInfo>> GetEpisodesAsync(AnimeModel anime)
    {
        return await anime.GetEpisodes();
    }

    public async Task<List<CharacterModel>> GetCharactersAsync(long animeId)
    {
        return await AnilistHelper.GetCharactersAsync(client, animeId);
    }

    public Task<List<string>> GetGenresAsync()
    {
        return Task.Run(() =>
        {
            lock (dbContext)
            {
                return dbContext.Anime
                                .Find(x => x.Genres.Count > 0)
                                .SelectMany(x => x.Genres)
                                .ToHashSet()
                                .ToList();
            }
        });
    }

    public async Task<List<AnimeModel>> GetPopularAnimeAsync(CancellationToken ct)
    {
        var ids = await AnilistHelper.GetPopularAnimeAsync(client, ct);
        return await Task.Run(() =>
        {
            lock (dbContext)
            {
                return dbContext.Anime.FindAll()
                                .Where(x => ids.Contains(x.AnilistId))
                                .ToList()
                                .Select(Converter.ToAppModel)
                                .ToList();
            }
        }, ct);
    }

    public async Task<List<AnimeModel>> GetUpcomingAnimeAsync(CancellationToken ct)
    {
        var ids = await AnilistHelper.GetUpcomingAnimeAsync(client, ct);
        return await Task.Run(() =>
        {
            lock (dbContext)
            {
                return dbContext.Anime.FindAll()
                                .Where(x => ids.Contains(x.AnilistId))
                                .ToList()
                                .Select(Converter.ToAppModel)
                                .ToList();
            }
        }, ct);
    }

    public async Task<List<AnimeModel>> GetAiringToday(CancellationToken ct)
    {
        var ids = await AnilistHelper.GetAiringToday(client, ct);
        return await Task.Run(() =>
        {
            lock (dbContext)
            {
                return dbContext.Anime.FindAll()
                                .Where(x => ids.Contains(x.AnilistId))
                                .ToList()
                                .Select(Converter.ToAppModel)
                                .ToList();
            }
        }, ct);
    }

    public async Task<List<AnimeModel>> BuilderRelationshipsAsync(long id, CancellationToken ct)
    {
        var anime = await GetAnimeWithoutAdditionalInfoAsync(id);
        if (anime is null)
        {
            return [];
        }

        var ids = new HashSet<long> { id };
        var relations = new List<AnimeModel> { anime };
        await BuildRelationshipsInternalAsync(anime, ids, relations, ct);
        return
        [
            .. relations
               .Where(x => x.Season is not null)
               .OrderBy(x => x.Season!.Year).ThenBy(x => x.Season!.SeasonName)
        ];
    }

    private async Task BuildRelationshipsInternalAsync(AnimeModel anime, HashSet<long> ids, List<AnimeModel> relations, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return;
        }

        if (await mappingService.GetId(anime) is not { MyAnimeList: > 0, Anilist: > 0 })
        {
            return;
        }

        foreach (var related in anime.Related)
        {
            if (!ids.Add(related.Id))
            {
                continue;
            }

            var relatedFull = await GetAnimeWithoutAdditionalInfoAsync(related.Id);

            if (relatedFull is null)
            {
                continue;
            }

            if (relatedFull.MediaFormat is AnimeMediaFormat.Special or AnimeMediaFormat.Music or AnimeMediaFormat.Ona)
            {
                continue;
            }

            relations.Add(relatedFull);

            await BuildRelationshipsInternalAsync(relatedFull, ids, relations, ct);
        }
    }
}