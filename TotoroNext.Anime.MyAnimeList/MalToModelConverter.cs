using System.Globalization;
using MalApi;
using TotoroNext.Anime.Abstractions;
using AiringStatus = TotoroNext.Anime.Abstractions.AiringStatus;
using AnimeSeason = TotoroNext.Anime.Abstractions.Models.AnimeSeason;
using Season = TotoroNext.Anime.Abstractions.Models.Season;

namespace TotoroNext.Anime.MyAnimeList;

public static class MalToModelConverter
{
    public static AnimeModel ConvertJikanModel(JikanDotNet.Anime jikanModel)
    {
        return new AnimeModel
        {
            Id = jikanModel.MalId ?? long.MinValue,
            ExternalIds = new ExternalIds
            {
                MyAnimeList = jikanModel.MalId ?? long.MinValue,
            },
            Title = GetTitle(jikanModel, "English"),
            Image = jikanModel.Images.JPG.ImageUrl,
            ServiceId = Module.Id,
            ServiceName = nameof(ExternalIds.MyAnimeList),
            Description = jikanModel.Synopsis,
            Url = $"https://myanimelist.net/anime/{jikanModel.MalId}/",
            MeanScore = (float?)jikanModel.Score
        };

        string GetTitle(JikanDotNet.Anime model, string type)
        {
            return model.Titles.FirstOrDefault(x => x.Type == type)?.Title ?? model.Titles.FirstOrDefault(x => x.Type == "Default")?.Title ?? "";
        }
    }
    
    public static AnimeModel ConvertModel(MalApi.Anime malModel)
    {
        var engTitle = malModel.AlternativeTitles?.English;

        var model = new AnimeModel
        {
            Id = malModel.Id,
            ExternalIds = new ExternalIds
            {
                MyAnimeList = malModel.Id
            },
            Title = malModel.Title,
            EngTitle = string.IsNullOrEmpty(engTitle) ? malModel.Title : engTitle,
            RomajiTitle = malModel.Title,
            Image = malModel.MainPicture?.Large ?? string.Empty,
            ServiceId = Module.Id,
            ServiceName = nameof(ExternalIds.MyAnimeList),
            Description = malModel.Synopsis ?? string.Empty,
            Url = $"https://myanimelist.net/anime/{malModel.Id}/",
            MediaFormat = ConvertFormat(malModel.MediaType),
            Genres = malModel.Genres is not { } genres ? [] : [ ..genres.Select(x => x.Name)]
        };

        try
        {
            if (malModel.UserStatus is { } progress)
            {
                model.Tracking = new Tracking
                {
                    WatchedEpisodes = progress.WatchedEpisodes,
                    Status = progress.IsRewatching ? ListItemStatus.Rewatching : (ListItemStatus)(int)progress.Status,
                    Score = (int)progress.Score,
                    UpdatedAt = progress.UpdatedAt,
                    StartDate = progress.StartDate,
                    FinishDate = progress.FinishDate
                };
            }

            model.AiringStatus = (AiringStatus)(int)(malModel.Status ?? MalApi.AiringStatus.NotYetAired);
            model.TotalEpisodes = malModel.TotalEpisodes;
            model.MeanScore = malModel.MeanScore;
            model.Popularity = malModel.Popularity ?? 0;
            if (malModel is { Broadcast: { DayOfWeek: not null } broadcast, Status: MalApi.AiringStatus.CurrentlyAiring })
            {
                var parts = broadcast.StartTime.Split(":");
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var hours) &&
                    int.TryParse(parts[1], out var minutes))
                {
                    var ts = new TimeSpan(hours, minutes, 0);
                    model.NextEpisodeAt = TimeUntilNext(broadcast.DayOfWeek.Value, ts);
                    if (DateTime.TryParseExact(malModel.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                    {
                        var localNow = DateTime.Now;
                        var jstZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
                        var jstNow = TimeZoneInfo.ConvertTime(localNow, jstZone);
                        model.AiredEpisodes = CalculateAiredEpisodes(dt.Add(ts), jstNow);
                    }
                }
            }


            //if (malModel.AlternativeTitles is { } alt)
            //{
            //    var titles = alt.Aliases.ToList();
            //    titles.Add(alt.English);
            //    titles.Add(alt.Japanese);
            //    model.AlternativeTitles = titles.Distinct();
            //}

            //if (malModel.Videos is { Length: > 0 } videos)
            //{
            //    model.Videos = malModel.Videos.Select(x => new Video
            //    {
            //        Id = x.Id,
            //        Thumbnail = x.Thumbnail,
            //        Title = x.Title,
            //        Url = x.Url,
            //    }).ToList();
            //}

            if (malModel.StartSeason is { } season)
            {
                model.Season = new Season((AnimeSeason)(int)season.SeasonName, season.Year);
            }

            if (malModel.RelatedAnime is { Length: > 0 } ra)
            {
                model.Related = ra.Select(x => ConvertModel(x.Anime)).ToArray();
            }

            if (malModel.Recommendations is { Length: > 0 } rec)
            {
                model.Recommended = rec.Select(x => ConvertModel(x.Anime)).ToArray();
            }
        }
        catch
        {
            // ignored
        }

        return model;
    }

    private static AnimeMediaFormat ConvertFormat(AnimeMediaType? malModelMediaType)
    {
        return malModelMediaType switch
        {
            AnimeMediaType.Unknown => AnimeMediaFormat.Unknown,
            AnimeMediaType.TV => AnimeMediaFormat.Tv,
            AnimeMediaType.OVA => AnimeMediaFormat.Ova,
            AnimeMediaType.Movie => AnimeMediaFormat.Movie,
            AnimeMediaType.Special => AnimeMediaFormat.Special,
            AnimeMediaType.ONA => AnimeMediaFormat.Ona,
            AnimeMediaType.Music => AnimeMediaFormat.Music,
            _ => AnimeMediaFormat.Unknown
        };
    }

    public static DateTime TimeUntilNext(DayOfWeek targetDay, TimeSpan targetTime)
    {
        var localNow = DateTime.Now;

        var jstZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
        var jstNow = TimeZoneInfo.ConvertTime(localNow, jstZone);

        var daysUntilTarget = ((int)targetDay - (int)jstNow.DayOfWeek + 7) % 7;

        if (daysUntilTarget == 0 && jstNow.TimeOfDay > targetTime)
        {
            daysUntilTarget = 7;
        }

        var jstTarget = jstNow.Date.AddDays(daysUntilTarget) + targetTime;
        return TimeZoneInfo.ConvertTime(jstTarget, jstZone, TimeZoneInfo.Local);
    }

    public static int CalculateAiredEpisodes(DateTime firstAirDateTime, DateTime currentTime)
    {
        // If nothing has aired yet
        if (currentTime < firstAirDateTime)
        {
            return 0;
        }

        // Calculate number of full weeks passed
        var elapsed = currentTime - firstAirDateTime;
        var weeksPassed = (int)(elapsed.TotalDays / 7);

        // Check if this week’s episode has aired
        var lastScheduled = firstAirDateTime.AddDays(weeksPassed * 7);
        if (currentTime >= lastScheduled)
        {
            weeksPassed++;
        }

        return weeksPassed;
    }
}