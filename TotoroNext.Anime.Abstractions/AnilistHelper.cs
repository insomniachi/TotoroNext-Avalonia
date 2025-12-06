using GraphQL;
using GraphQL.Client.Http;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Anime.Anilist;

namespace TotoroNext.Anime.Abstractions;

public static class AnilistHelper
{
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