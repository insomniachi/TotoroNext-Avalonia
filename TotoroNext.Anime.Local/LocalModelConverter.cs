using LiteDB;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Local;

internal static class LocalModelConverter
{
    public static AnimeModel ToAnimeModel(LocalAnimeModel anime, ILiteCollection<LocalAnimeModel> collection)
    {
        var model = ToAnimeModel(anime);
        model.Related = anime.Related
                             .Select(id => collection.Include(x => x.Tracking).FindById(id))
                             .Where(x => x is not null)
                             .Select(ToAnimeModel)
                             .ToList();
        return model;
    }

    public static AnimeModel ToAnimeModel(LocalAnimeModel anime)
    {
        var model =  new AnimeModel
        {
            Title = anime.Title,
            TotalEpisodes = anime.AiringStatus == AiringStatus.FinishedAiring ? anime.TotalEpisodes : null,
            Genres = anime.Genres.ToList(),
            Season = anime.Season,
            MeanScore = anime.MeanScore,
            Studios = anime.Studios,
            AiringStatus = anime.AiringStatus,
            Image = anime.Image,
            BannerImage = string.IsNullOrEmpty(anime.AdditionalInfo?.Info.BannerImage) ? anime.Image : anime.AdditionalInfo.Info.BannerImage,
            ServiceName = "Local",
            ServiceId = Guid.Empty,
            Id = anime.MyAnimeListId,
            ExternalIds = new AnimeId
            {
                Anilist = anime.AnilistId,
                MyAnimeList = anime.MyAnimeListId,
                Kitsu = anime.KitsuId,
                AniDb = anime.AniDbId,
                Simkl = anime.SimklId,
            },
            Episodes = anime.EpisodeInfo?.Info ?? [],
            Tracking = anime.Tracking?.Tracking,
            Url = $"https://myanimelist.net/anime/{anime.MyAnimeListId}/",
            MediaFormat = anime.MediaFormat
        };

        if (anime.AdditionalInfo is not { } info)
        {
            return model;
        }

        model.Popularity = info.Info.Popularity;
        model.Description = info.Info.Description;
        model.Trailers = [..info.Info.Videos];
        model.EngTitle = info.Info.TitleEnglish;
        model.RomajiTitle = info.Info.TitleRomaji;

        return model;
    }


    public static LocalAnimeModel Convert(Anime anime)
    {
        var model = new LocalAnimeModel
        {
            Title = anime.Title,
            TotalEpisodes = anime.Episodes,
            Genres = anime.Tags.ToList(),
            Season = ConvertSeason(anime.AnimeSeason),
            MeanScore = (float)Math.Round(anime.Score?.ArithmeticMean ?? 0, 2),
            Studios = anime.Studios,
            AiringStatus = ConvertStatus(anime.Status),
            MediaFormat = ConvertMediaFormat(anime.Type),
            Related = ConvertRelated(anime.RelatedAnime).ToList(),
            Image = anime.Picture,
            Thumbnail = anime.Thumbnail
        };

        UpdateIds(model, anime.Sources);

        return model;
    }

    private static AnimeMediaFormat ConvertMediaFormat(string animeType)
    {
        return animeType switch
        {
            "MOVIE" => AnimeMediaFormat.Movie,
            "SPECIAL" => AnimeMediaFormat.Special,
            "OVA" => AnimeMediaFormat.Ova,
            "TV" => AnimeMediaFormat.Tv,
            "MUSIC" => AnimeMediaFormat.Music,
            "ONA" => AnimeMediaFormat.Ona,
            _ => AnimeMediaFormat.Unknown
        };
    }

    private static IEnumerable<long> ConvertRelated(List<string> related)
    {
        return related.Where(x => x.StartsWith("https://myanimelist.net/"))
                      .Select(url => url.Split('/').LastOrDefault())
                      .OfType<string>()
                      .Select(long.Parse);
    }

    private static AiringStatus ConvertStatus(string animeStatus)
    {
        return animeStatus switch
        {
            "FINISHED" => AiringStatus.FinishedAiring,
            "ONGOING" => AiringStatus.CurrentlyAiring,
            _ => AiringStatus.NotYetAired
        };
    }

    private static Season? ConvertSeason(OfflineDbAnimeSeason? season)
    {
        if (season is null)
        {
            return null;
        }

        AnimeSeason? name = season.Season switch
        {
            "WINTER" => AnimeSeason.Winter,
            "SPRING" => AnimeSeason.Spring,
            "SUMMER" => AnimeSeason.Summer,
            "FALL" => AnimeSeason.Fall,
            _ => null
        };

        return name is null ? null : new Season(name.Value, season.Year);
    }

    private static void UpdateIds(LocalAnimeModel model, IEnumerable<string> sources)
    {
        foreach (var source in sources)
        {
            if (string.IsNullOrEmpty(source))
            {
                continue;
            }

            var serviceId = source.Split('/').LastOrDefault();

            if (string.IsNullOrEmpty(serviceId))
            {
                continue;
            }

            if (source.StartsWith("https://anidb.net/"))
            {
                model.AniDbId = long.Parse(serviceId);
            }
            else if (source.StartsWith("https://anilist.co/"))
            {
                model.AnilistId = long.Parse(serviceId);
            }
            else if (source.StartsWith("https://kitsu.app/"))
            {
                model.KitsuId = long.Parse(serviceId);
            }
            else if (source.StartsWith("https://myanimelist.net/"))
            {
                model.MyAnimeListId = long.Parse(serviceId);
            }
            else if (source.StartsWith("https://simkl.com/"))
            {
                model.SimklId = long.Parse(serviceId);
            }
        }
    }
}