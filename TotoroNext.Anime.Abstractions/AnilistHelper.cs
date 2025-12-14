using GraphQL;
using GraphQL.Client.Http;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Anilist;

namespace TotoroNext.Anime.Abstractions;

public static class AnilistHelper
{
    public static async Task<int> GetTotalAiredEpisodes(GraphQLHttpClient client, long anilistId)
    {
        var query = new QueryQueryBuilder().WithMedia(new MediaQueryBuilder()
                                                      .WithStatus()
                                                      .WithEpisodes()
                                                      .WithNextAiringEpisode(new AiringScheduleQueryBuilder()
                                                                                 .WithEpisode()), (int)anilistId).Build();
        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = query
        });

        if (response.Data.Media.Status == MediaStatus.Finished)
        {
            return response.Data.Media.Episodes ?? -1;
        }

        return (response.Data.Media.NextAiringEpisode.Episode ?? 0) - 1;
    }

    public static async Task<(int AiredEpisodes, DateTime? NextAiringAt)> GetNextEpisodeInfo(GraphQLHttpClient client, long anilistId)
    {
        var query = new QueryQueryBuilder().WithMedia(new MediaQueryBuilder()
                                                          .WithNextAiringEpisode(new AiringScheduleQueryBuilder()
                                                                                 .WithTimeUntilAiring()
                                                                                 .WithEpisode()), (int)anilistId).Build();
        var response = await client.SendQueryAsync<Query>(new GraphQLRequest
        {
            Query = query
        });

        return new ValueTuple<int, DateTime?>(response.Data.Media.NextAiringEpisode.Episode ?? 0,
                                              ConvertToExactTime(response.Data.Media.NextAiringEpisode.TimeUntilAiring));
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
    
    public static async Task<List<long>> GetPopularAnimeAsync(GraphQLHttpClient client)
    {
        try
        {
            var response = await client.SendQueryAsync<Query>(new GraphQLRequest
            {
                Query = new QueryQueryBuilder().WithPage(new PageQueryBuilder()
                                                             .WithMedia(new MediaQueryBuilder()
                                                                            .WithId(),
                                                                        sort: new List<MediaSort?> { MediaSort.TrendingDesc },
                                                                        status: MediaStatus.Releasing,
                                                                        type: MediaType.Anime), 1, 15)
                                               .Build()
            });

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