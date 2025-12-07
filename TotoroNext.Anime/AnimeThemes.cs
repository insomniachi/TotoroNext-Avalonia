using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime;

internal class AnimeThemes(IHttpClientFactory httpClientFactory) : IAnimeThemes
{
    public async Task<List<Abstractions.AnimeTheme>> FindById(long id, string serviceName)
    {
        try
        {
            if (serviceName == "Local")
            {
                serviceName = nameof(AnimeId.Anilist);
            }
            
            using var client = httpClientFactory.CreateClient();
            var searchResponse =
                await
                    client.GetFromJsonAsync<AnimeThemesSearchResponse>($"https://api.animethemes.moe/anime?filter[has]=resources&filter[site]={serviceName}&filter[external_id]={id}");
            if (searchResponse?.Anime is not { Count: 1 })
            {
                return [];
            }

            var slug = searchResponse.Anime[0].Slug;
            var animeResponse =
                await
                    client.GetFromJsonAsync<AnimeThemesResponse>($"https://api.animethemes.moe/anime/{slug}?include=animethemes.animethemeentries.videos.audio,animethemes.song.artists");
            return animeResponse?.Anime.AnimeThemes.Select(x => new Abstractions.AnimeTheme
            {
                Type = Enum.Parse<AnimeThemeType>(x.Type),
                Slug = x.Slug,
                SongName = x.Song.Title,
                Video = x.AnimeThemeEntries.SelectMany(entry => entry.Videos).MaxBy(video => video.Resolution)?.Link is { } link
                    ? new Uri(link)
                    : null,
                Audio = x.AnimeThemeEntries.SelectMany(entry => entry.Videos.Select(video => video.Audio)).FirstOrDefault() is
                    { Link: not null } audio
                    ? new Uri(audio.Link)
                    : null,
                Artist = string.Join(",", x.Song.Artists.Select(artist => artist.Name))
            }).ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }
}

[Serializable]
internal class AnimeThemesSearchResponse
{
    [JsonPropertyName("anime")] public List<AnimeItem> Anime { get; set; } = [];
}

[Serializable]
internal class AnimeThemesResponse
{
    [JsonPropertyName("anime")] public AnimeItem Anime { get; set; } = new();
}

[Serializable]
internal class AnimeItem
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("media_format")] public string MediaFormat { get; set; } = string.Empty;

    [JsonPropertyName("season")] public string Season { get; set; } = string.Empty;

    [JsonPropertyName("slug")] public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("synopsis")] public string Synopsis { get; set; } = string.Empty;

    [JsonPropertyName("year")] public int Year { get; set; }

    [JsonPropertyName("animethemes")] public List<AnimeTheme> AnimeThemes { get; set; } = [];
}

[Serializable]
internal class AnimeTheme
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("sequence")] public int? Sequence { get; set; }

    [JsonPropertyName("slug")] public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("song")] public AnimeSong Song { get; set; } = new();

    [JsonPropertyName("animethemeentries")]
    public List<AnimeThemeEntry> AnimeThemeEntries { get; set; } = [];
}

[Serializable]
internal class AnimeSong
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    [JsonPropertyName("artists")] public List<AnimeArtist> Artists { get; set; } = [];
}

[Serializable]
internal class AnimeArtist
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
}

[Serializable]
internal class AnimeThemeEntry
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("episodes")] public string Episodes { get; set; } = string.Empty;

    [JsonPropertyName("notes")] public string? Notes { get; set; }

    [JsonPropertyName("nsfw")] public bool Nsfw { get; set; }

    [JsonPropertyName("spoiler")] public bool Spoiler { get; set; }

    [JsonPropertyName("version")] public int? Version { get; set; }

    [JsonPropertyName("videos")] public List<AnimeThemeVideo> Videos { get; set; } = [];
}

[Serializable]
internal class AnimeThemeVideo
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("basename")] public string Basename { get; set; } = string.Empty;

    [JsonPropertyName("filename")] public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("lyrics")] public bool Lyrics { get; set; }

    [JsonPropertyName("nc")] public bool Nc { get; set; }

    [JsonPropertyName("overlap")] public string Overlap { get; set; } = string.Empty;

    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;

    [JsonPropertyName("resolution")] public int Resolution { get; set; }

    [JsonPropertyName("size")] public long Size { get; set; }

    [JsonPropertyName("source")] public string Source { get; set; } = string.Empty;

    [JsonPropertyName("subbed")] public bool Subbed { get; set; }

    [JsonPropertyName("uncen")] public bool Uncen { get; set; }

    [JsonPropertyName("tags")] public string Tags { get; set; } = string.Empty;

    [JsonPropertyName("link")] public string Link { get; set; } = string.Empty;

    [JsonPropertyName("audio")] public AnimeAudio Audio { get; set; } = new();
}

[Serializable]
internal class AnimeAudio
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("basename")] public string Basename { get; set; } = string.Empty;

    [JsonPropertyName("filename")] public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;

    [JsonPropertyName("size")] public long Size { get; set; }

    [JsonPropertyName("link")] public string? Link { get; set; } = string.Empty;
}