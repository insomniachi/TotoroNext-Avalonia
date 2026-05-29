using System.Text.Json;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime;

internal class AnimeMusicService(
    IHttpClientFactory httpClientFactory,
    IAnimeMappingService animeMappingService) : IAnimeMusicService
{
    public async Task<List<AnimeMusic>> FindAll(AnimeModel anime)
    {
        try
        {
            var id = GetAnnId(anime);

            if (id == 0)
            {
                return [];
            }
            
            using var client = new FlurlClient(httpClientFactory.CreateClient());
            var stream = await client.Request("https://anisongdb.com/api/ann_ids_request")
                                     .PostJsonAsync(new
                                     {
                                         ann_ids = new[] { id },
                                         ending_filter = true,
                                         ignore_duplicate = true,
                                         insert_filter = true,
                                         opening_filter = true
                                     })
                                     .ReceiveStream();
            var doc = await JsonDocument.ParseAsync(stream);
            
            return doc.RootElement.EnumerateArray()
                      .Select(item => new AnimeMusic
                      {
                          SongName = item.GetProperty("songName").GetString() ?? "",
                          Artist = item.GetProperty("songArtist").GetString() ?? "",
                          Type = item.GetProperty("songType").GetString() ?? "",
                          Audio = GetAudio(item),
                          Video = GetVideo(item)
                      }).ToList();
        }
        catch
        {
            return [];
        }
    }

    private long GetAnnId(AnimeModel anime)
    {
        if (anime.ExternalIds is { AnimeNewsNetwork: > 0 })
        {
            return anime.ExternalIds.AnimeNewsNetwork;
        }

        var id = animeMappingService.GetId(anime);

        if (id is null)
        {
            return 0;
        }

        return id.AnimeNewsNetwork;
    }

    private static Uri? GetVideo(JsonElement item)
    {
        var slug = item.GetProperty("HQ").GetString();

        if (slug is not null)
        {
            return GetUrl(slug);
        }

        slug = item.GetProperty("MQ").GetString();
        return slug is null ? null : GetUrl(slug);
    }

    private static Uri? GetAudio(JsonElement item)
    {
        var slug = item.GetProperty("audio").GetString();
        return slug is null ? null : GetUrl(slug);
    }

    private static Uri GetUrl(string slug)
    {
        return new Uri($"https://naedist.animemusicquiz.com/{slug}");
    }
}