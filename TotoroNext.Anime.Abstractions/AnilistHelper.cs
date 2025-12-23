using GraphQL;
using GraphQL.Client.Http;
using Microsoft.Extensions.Caching.Memory;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Anilist;

namespace TotoroNext.Anime.Abstractions;

public static class AnilistHelper
{
    private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
    
    public static async ValueTask<int> GetTotalAiredEpisodes(GraphQLHttpClient client, long anilistId, CancellationToken ct)
    {
        var key = $"{anilistId}tae";
        if (Cache.TryGetValue<int>(key, out var cachedValue))
        {
            return cachedValue;
        }
        
        var query = new QueryQueryBuilder().WithMedia(new MediaQueryBuilder()
                                                      .WithStatus()
                                                      .WithEpisodes()
                                                      .WithNextAiringEpisode(new AiringScheduleQueryBuilder()
                                                                                 .WithEpisode()), (int)anilistId).Build();
        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = query
        }, ct);

        if (response.Data.Media.Status == MediaStatus.Finished)
        {
            return response.Data.Media.Episodes ?? -1;
        }

        var value = (response.Data.Media.NextAiringEpisode.Episode ?? 0) - 1;
        Cache.Set(key, value, TimeSpan.FromMinutes(2));
        return value;
    }

    public static async ValueTask<(int AiredEpisodes, DateTime? NextAiringAt)> GetNextEpisodeInfo(GraphQLHttpClient client, long anilistId, CancellationToken ct)
    {
        var key = $"{anilistId}tae+at";
        if (Cache.TryGetValue<(int, DateTime?)>(key, out var cachedValue))
        {
            return cachedValue;
        }
        
        var query = new QueryQueryBuilder().WithMedia(new MediaQueryBuilder()
                                                          .WithNextAiringEpisode(new AiringScheduleQueryBuilder()
                                                                                 .WithTimeUntilAiring()
                                                                                 .WithEpisode()), (int)anilistId).Build();
        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = query
        }, ct);

        var value = new ValueTuple<int, DateTime?>(response.Data.Media.NextAiringEpisode.Episode ?? 0,
                                              ConvertToExactTime(response.Data.Media.NextAiringEpisode.TimeUntilAiring));
        Cache.Set(key, value, TimeSpan.FromMinutes(2));
        return value;
    }

    public static async Task<List<CharacterModel>> GetCharactersAsync(GraphQLHttpClient client, long anilistId)
    {
        var query = new QueryQueryBuilder().WithMedia(new MediaQueryBuilder()
                                                          .WithCharacters(new CharacterConnectionQueryBuilder()
                                                                              .WithNodes(new CharacterQueryBuilder()
                                                                                         .WithName(new CharacterNameQueryBuilder()
                                                                                                       .WithFull())
                                                                                         .WithImage(new CharacterImageQueryBuilder()
                                                                                                        .WithLarge()))), (int)anilistId,
                                                      type: MediaType.Anime).Build();

        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = query
        });

        if (response.Data.Media.Characters is null)
        {
            return [];
        }

        return response.Data.Media.Characters.Nodes.Select(x => new CharacterModel
        {
            Name = x.Name.Full,
            Image = TryConvertUri(x.Image.Large)
        }).ToList();
    }

    public static async Task<List<long>> GetPopularAnimeAsync(GraphQLHttpClient client, CancellationToken ct)
    {
        try
        {
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithMedia(new MediaQueryBuilder()
                                                                            .WithId(),
                                                                        sort: new List<MediaSort?>
                                                                            { MediaSort.TrendingDesc, MediaSort.PopularityDesc },
                                                                        status: MediaStatus.Releasing,
                                                                        type: MediaType.Anime), 1, 20)
                                               .Build()
            }, ct);

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            return [.. response.Data.Page.Media.Select(x => x.Id).Where(x => x is not null).Select(x => x!.Value)];
        }
        catch
        {
            return [];
        }
    }

    public static async Task<List<long>> GetUpcomingAnimeAsync(GraphQLHttpClient client, CancellationToken ct)
    {
        try
        {
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithMedia(new MediaQueryBuilder()
                                                                            .WithId(),
                                                                        sort: new List<MediaSort?> { MediaSort.PopularityDesc },
                                                                        status: MediaStatus.NotYetReleased,
                                                                        type: MediaType.Anime), 1, 20)
                                               .Build()
            }, ct);

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            return [.. response.Data.Page.Media.Select(x => x.Id).Where(x => x is not null).Select(x => x!.Value)];
        }
        catch
        {
            return [];
        }
    }

    public static async Task<List<long>> GetAiringToday(GraphQLHttpClient client, CancellationToken ct)
    {
        try
        {
            var start = (int)((DateTimeOffset)DateTime.UtcNow.Date).ToUnixTimeSeconds();
            var end = (int)((DateTimeOffset)DateTime.UtcNow.Date.AddDays(1)).ToUnixTimeSeconds();
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithAiringSchedules(new AiringScheduleQueryBuilder()
                                                                                      .WithMedia(new MediaQueryBuilder()
                                                                                                     .WithId()),
                                                                                  airingAtGreater: start,
                                                                                  airingAtLesser: end,
                                                                                  sort: new List<AiringSort?> { AiringSort.Time }),1,20)
                                               .Build()
            }, ct);

            if (response.Errors?.Length > 0)
            {
                return [];
            }

            return [.. response.Data.Page.AiringSchedules.Select(x => x.Media.Id).Where(x => x is not null).Select(x => x!.Value)];
        }
        catch
        {
            return [];
        }
    }

    private static Uri? TryConvertUri(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null;
    }

    public static DateTime? ConvertToExactTime(int? secondsTillAiring)
    {
        if (secondsTillAiring is null)
        {
            return null;
        }

        return DateTime.Now + TimeSpan.FromSeconds(secondsTillAiring.Value);
    }
}