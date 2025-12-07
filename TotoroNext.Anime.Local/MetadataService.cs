using Flurl;
using FuzzySharp;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Anilist;

namespace TotoroNext.Anime.Local;

internal class MetadataService(ILiteDbContext dbContext) : IMetadataService
{
    private static readonly Lazy<GraphQLHttpClient> ClientLazy =
        new(new GraphQLHttpClient("https://graphql.anilist.co/", new NewtonsoftJsonSerializer(), new HttpClient()));

    public Guid Id => Guid.Empty;

    public string Name => "Local";

    public async Task<AnimeModel> GetAnimeAsync(long id)
    {
        var tcs = new TaskCompletionSource<AnimeModel>();

        await Task.Run(async () =>
        {
            var anime = dbContext.Anime.FindById(id);
            if (anime.AdditionalInfo is null)
            {
                var query = new QueryQueryBuilder().WithMedia(MediaQueryBuilderFull(), (int)id,
                                                              type: MediaType.Anime).Build();
                var response = await ClientLazy.Value.SendQueryAsync<Query>(new GraphQLRequest
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
                        Videos = [..ConvertTrailers(response.Data.Media.Trailer)]
                    },
                    ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)
                };

                dbContext.AdditionalInfo.Upsert(anime.AdditionalInfo);
                dbContext.Anime.Upsert(anime);
            }

            tcs.SetResult(LocalModelConverter.ToAnimeModel(anime, dbContext.Anime));
        });

        return await tcs.Task;
    }

    public async Task<List<AnimeModel>> SearchAnimeAsync(string term)
    {
        var tcs = new TaskCompletionSource<List<AnimeModel>>();

        await Task.Run(() =>
        {
            var prefix = term.Length >= 3 ? term[..3] : term;
            var candidates = dbContext.Anime.Find(LiteDB.Query.Contains("Title", prefix)).Take(50);
            var results = candidates
                          .Select(a => new { Anime = a, Score = Fuzz.PartialRatio(a.Title, term) })
                          .Where(x => x.Score >= 70)
                          .OrderByDescending(x => x.Score)
                          .Select(x => x.Anime)
                          .Select(x => LocalModelConverter.ToAnimeModel(x, dbContext.Anime)).ToList();
            tcs.SetResult(results);
        });

        return await tcs.Task;
    }

    public async Task<List<AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request)
    {
        if (request.IsEmpty())
        {
            return [];
        }

        var tcs = new TaskCompletionSource<List<AnimeModel>>();

        await Task.Run(() =>
        {
            var term = request.Title;
            var candidates = string.IsNullOrEmpty(term)
                ? dbContext.Anime
                           .FindAll()
                : dbContext.Anime
                           .Find(LiteDB.Query.Contains("Title", term.Length >= 3 ? term[..3] : term))
                           .Select(a => new { Anime = a, Score = Fuzz.PartialRatio(a.Title, term) })
                           .Where(x => x.Score >= 70)
                           .OrderByDescending(x => x.Score)
                           .Select(x => x.Anime);

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

            tcs.SetResult(response);
        });

        return await tcs.Task;
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
            ExpiresAt = DateTimeOffset.Now.AddDays(3)
        };

        if (anime.AiringStatus == AiringStatus.FinishedAiring)
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
            Characters = await AnilistHelper.GetCharactersAsync(ClientLazy.Value, animeId),
            ExpiresAt = DateTimeOffset.Now.AddDays(3)
        };

        dbContext.Characters.Upsert(anime.CharacterInfo);
        dbContext.Anime.Upsert(anime);

        return anime.CharacterInfo.Characters;
    }

    public async Task<List<string>> GetGenresAsync()
    {
        var tcs = new TaskCompletionSource<List<string>>();

        await Task.Run(() =>
        {
            var genres = dbContext.Anime
                                  .Find(x => x.Genres.Count > 0)
                                  .SelectMany(x => x.Genres)
                                  .ToHashSet();
            tcs.SetResult([..genres]);
        });

        return await tcs.Task;
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