using Flurl;
using FuzzySharp;
using GraphQL;
using GraphQL.Client.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Anilist;

namespace TotoroNext.Anime.Local;

internal class MetadataService(ILiteDbContext dbContext, GraphQLHttpClient client) : IMetadataService
{
    public Guid Id => Guid.Empty;

    public string Name => "Local";

    public Task<AnimeModel> GetAnimeAsync(long id)
    {
        return Task.Run(async () =>
        {
            var anime = dbContext.Anime.FindById(id);

            if (anime.AdditionalInfo is not null)
            {
                return LocalModelConverter.ToAnimeModel(anime, dbContext.Anime);
            }

            var query = new QueryQueryBuilder().WithMedia(MediaQueryBuilderFull(), (int)anime.AnilistId,
                                                          type: MediaType.Anime).Build();
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = query
            });

            anime.AdditionalInfo = new LocalAdditionalInfo
            {
                Id = id,
                Info = new OfflineAdditionalInfo
                {
                    TitleEnglish = response.Data.Media.Title.English,
                    TitleRomaji = response.Data.Media.Title.Romaji,
                    Description = response.Data.Media.Description,
                    Popularity = response.Data.Media.Popularity ?? 0,
                    Videos = [..ConvertTrailers(response.Data.Media.Trailer)],
                    BannerImage = response.Data.Media.BannerImage
                },
                ExpiresAt = DateTimeOffset.UtcNow.AddMonths(1)
            };

            dbContext.AdditionalInfo.Upsert(anime.AdditionalInfo);
            dbContext.Anime.Upsert(anime);

            return LocalModelConverter.ToAnimeModel(anime, dbContext.Anime);
        });
    }

    public Task<List<AnimeModel>> SearchAnimeAsync(string term)
    {
        return Task.Run(() =>
        {
            var results = dbContext.Anime.FindAll().Select(x =>
                                   {
                                       var titleScore = Fuzz.TokenSetRatio(term, x.Title.ToLower());
                                       var altScore = x.AlternateTitles.Count != 0
                                           ? x.AlternateTitles.Max(t => Fuzz.TokenSetRatio(term, t.ToLower()))
                                           : 0;
                                       var bestScore = Math.Max(titleScore, altScore);
                                       return (Anime: x, Score: bestScore);
                                   })
                                   .Where(x => x.Score >= 85)
                                   .OrderByDescending(x => x.Score)
                                   .Select(x => x.Anime)
                                   .Take(15)
                                   .Select(x => LocalModelConverter.ToAnimeModel(x, dbContext.Anime))
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
                                           var titleScore = Fuzz.TokenSetRatio(term, x.Title.ToLower());
                                           var altScore = x.AlternateTitles.Count != 0
                                               ? x.AlternateTitles.Max(t => Fuzz.TokenSetRatio(term, t.ToLower()))
                                               : 0;
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
                                     .Select(x => LocalModelConverter.ToAnimeModel(x, dbContext.Anime))
                                     .ToList();

            return response;
        });
    }

    public async Task<List<EpisodeInfo>> GetEpisodesAsync(AnimeModel anime)
    {
        if (anime.Episodes is { Count: > 0 })
        {
            return anime.Episodes;
        }

        var localAnime = dbContext.Anime.FindById(anime.Id);
        localAnime.EpisodeInfo = new LocalEpisodeInfo
        {
            Id = anime.Id,
            Info = await anime.GetEpisodes(),
            ExpiresAt = DateTimeOffset.Now.AddDays(6)
        };

        if (anime.AiringStatus == AiringStatus.FinishedAiring && localAnime.EpisodeInfo.Info is { Count: > 0 })
        {
            dbContext.Episodes.Upsert(localAnime.EpisodeInfo);
            dbContext.Anime.Upsert(localAnime);
        }

        return localAnime.EpisodeInfo.Info;
    }

    public async Task<List<CharacterModel>> GetCharactersAsync(long animeId)
    {
        var anime = dbContext.Anime.FindById(animeId);

        if (anime.CharacterInfo is { Characters.Count: > 0 })
        {
            return anime.CharacterInfo.Characters;
        }

        anime.CharacterInfo = new LocalCharacterInfo
        {
            Id = animeId,
            Characters = await AnilistHelper.GetCharactersAsync(client, anime.AnilistId),
            ExpiresAt = DateTimeOffset.Now.AddDays(1)
        };

        dbContext.Characters.Upsert(anime.CharacterInfo);
        dbContext.Anime.Upsert(anime);

        return anime.CharacterInfo.Characters;
    }

    public Task<List<string>> GetGenresAsync()
    {
        return Task.Run(() =>
        {
            return dbContext.Anime
                            .Find(x => x.Genres.Count > 0)
                            .SelectMany(x => x.Genres)
                            .ToHashSet()
                            .ToList();
        });
    }

    public async Task<List<AnimeModel>> GetPopularAnimeAsync(CancellationToken ct)
    {
        var ids = await AnilistHelper.GetPopularAnimeAsync(client, ct);
        return await Task.Run(() =>
        {
            return dbContext.Anime.FindAll()
                            .Where(x => ids.Contains(x.AnilistId))
                            .Select(LocalModelConverter.ToAnimeModel)
                            .ToList();
        }, ct);
    }

    public async Task<List<AnimeModel>> GetUpcomingAnimeAsync(CancellationToken ct)
    {
        var ids = await AnilistHelper.GetUpcomingAnimeAsync(client, ct);
        return await Task.Run(() =>
        {
            return dbContext.Anime.FindAll()
                            .Where(x => ids.Contains(x.AnilistId))
                            .Select(LocalModelConverter.ToAnimeModel)
                            .ToList();
        }, ct);
    }

    public async Task<List<AnimeModel>> GetAiringToday(CancellationToken ct)
    {
        var ids = await AnilistHelper.GetAiringToday(client, ct);
        return await Task.Run(() =>
        {
            return dbContext.Anime.FindAll()
                            .Where(x => ids.Contains(x.AnilistId))
                            .Select(LocalModelConverter.ToAnimeModel).ToList();
        }, ct);
    }

    private static MediaQueryBuilder MediaQueryBuilderFull()
    {
        return new MediaQueryBuilder()
               .WithId()
               .WithTitle(new MediaTitleQueryBuilder()
                          .WithEnglish()
                          .WithNative()
                          .WithRomaji())
               .WithPopularity()
               .WithDescription(false)
               .WithTrailer(new MediaTrailerQueryBuilder()
                            .WithSite()
                            .WithThumbnail()
                            .WithId());
    }

    private static IReadOnlyCollection<TrailerVideo> ConvertTrailers(MediaTrailer? mediaTrailer)
    {
        if (mediaTrailer is null)
        {
            return [];
        }

        if (mediaTrailer.Site.Equals("youtube", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new TrailerVideo
                {
                    Url = "https://www.youtube.com/watch".AppendQueryParam("v", mediaTrailer.Id),
                    Title = "Trailer",
                    Thumbnail = mediaTrailer.Thumbnail
                }
            ];
        }

        return [];
    }
}