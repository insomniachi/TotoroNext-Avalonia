using System.Text;
using System.Text.Json;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime;

internal class AnimeMusicService(
    IHttpClientFactory httpClientFactory,
    IAnimeMappingService animeMappingService) : IAnimeMusicService
{
    internal const string Query = """
                                  query Query($site: ResourceSite!, $findAnimeByExternalSiteId: [Int!]) {
                                    findAnimeByExternalSite(site: $site, id: $findAnimeByExternalSiteId) {
                                      animethemes {
                                        animethemeentries {
                                          videos {
                                            nodes {
                                              audio {
                                                link
                                              }
                                              link
                                            }
                                          }
                                        }
                                        slug
                                        song {
                                          title {
                                            romaji
                                            native
                                          }
                                          performances {
                                            artist {
                                              name {
                                                main
                                              }
                                            }
                                          }
                                        }
                                      }
                                    }
                                  }
                                  """;

    public async Task<List<AnimeMusic>> FindAll(AnimeModel anime)
    {
        try
        {
            return await FetchFromAnimeThemes(anime);
        }
        catch (Exception)
        {
            return [];
        }
    }

    private async Task<List<AnimeMusic>> FetchFromAnimeThemes(AnimeModel anime)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation(HeaderNames.UserAgent, Http.UserAgent);

        var variables = new
        {
            site = "ANILIST",
            findAnimeByExternalSiteId = anime.ExternalIds.Anilist
        };
        var payload = new
        {
            query = Query,
            variables
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://graphql.animethemes.moe", content);
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(stream);
        return doc.RootElement.GetProperty("data")
                  .GetProperty("findAnimeByExternalSite")
                  .EnumerateArray().FirstOrDefault()
                  .GetProperty("animethemes").EnumerateArray()
                  .Select(ParseAnimeMusic)
                  .ToList();
    }

    // ReSharper disable once UnusedMember.Local
    private async Task<List<AnimeMusic>> FetchFromAnisongDb(AnimeModel anime)
    {
        var id = await GetAnnId(anime);

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


    private static AnimeMusic ParseAnimeMusic(JsonElement item)
    {
        var song = item.GetProperty("song");
        var entry = item.GetProperty("animethemeentries").EnumerateArray().First()
                        .GetProperty("videos").GetProperty("nodes").EnumerateArray().First();
        var audio = entry.GetProperty("audio").GetProperty("link").GetString();
        var video = entry.GetProperty("link").GetString();
        var perf = song.GetProperty("performances").EnumerateArray().ToList();

        var music = new AnimeMusic
        {
            Type = item.GetProperty("slug").GetString(),
            SongName = song.GetProperty("title").GetProperty("romaji").GetString()!
        };

        if (perf.Count > 0)
        {
            music.Artist = perf.FirstOrDefault().GetProperty("artist").GetProperty("name")
                               .GetProperty("main").GetString()!;
        }

        if (!string.IsNullOrEmpty(audio))
        {
            music.Audio = new Uri(audio);
        }

        if (!string.IsNullOrEmpty(video))
        {
            music.Video = new Uri(video);
        }

        return music;
    }

    private async Task<long> GetAnnId(AnimeModel anime)
    {
        if (anime.ExternalIds is { AnimeNewsNetwork: > 0 })
        {
            return anime.ExternalIds.AnimeNewsNetwork;
        }

        var id = await animeMappingService.GetId(anime);

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