using Flurl;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Jellyfin;

public class AnimeProvider(
    JellyfinApiClient client,
    IModuleSettings<Settings> settings) : IAnimeProvider
{
    public static string? SessionId { get; private set; }
    public static string? MediaSourceId { get; private set; }

    public async IAsyncEnumerable<SearchResult> SearchAsync(string query)
    {
        var result = await client.Items.GetAsync(x =>
        {
            var qp = x.QueryParameters;
            qp.SearchTerm = query;
            qp.SortBy = [ItemSortBy.SortName];
            qp.SortOrder = [SortOrder.Ascending];
            qp.Recursive = true;
            qp.Limit = 20;
            qp.IncludeItemTypes = [BaseItemKind.Series, BaseItemKind.Movie];
        });

        if (result is null)
        {
            yield break;
        }

        foreach (var item in result.Items ?? [])
        {
            var image = settings.Value.ServerUrl.AppendPathSegment($"/Items/{item.Id}/Images/Primary");
            yield return new SearchResult(this, $"{item.Id}", item.Name ?? "", new Uri(image));
        }
    }

    public async IAsyncEnumerable<VideoServer> GetServersAsync(string animeId, string episodeId)
    {
        var id = Guid.Parse(episodeId);
        var item = await client.Items[id].GetAsync(x => x.QueryParameters.UserId = Settings.UserId);
        if (item is null)
        {
            yield break;
        }

        if (await GetMediaUrl(item) is not { } url)
        {
            yield break;
        }


        var server = new VideoServer("Default", url);
        var segments = await client.MediaSegments[id]
                                   .GetAsync(x => x.QueryParameters.IncludeSegmentTypes = [MediaSegmentType.Intro, MediaSegmentType.Outro]);
        if (segments != null)
        {
            var skipData = new SkipData();
            foreach (var segment in segments.Items ?? [])
            {
                if (segment.StartTicks is not { } start || segment.EndTicks is not { } end)
                {
                    continue;
                }

                switch (segment.Type)
                {
                    case MediaSegmentDto_Type.Intro:
                        skipData.Opening = new Segment
                        {
                            Start = TimeSpan.FromTicks(start),
                            End = TimeSpan.FromTicks(end)
                        };
                        break;
                    case MediaSegmentDto_Type.Outro:
                        skipData.Ending = new Segment
                        {
                            Start = TimeSpan.FromTicks(start),
                            End = TimeSpan.FromTicks(end)
                        };
                        break;
                }
            }

            server.SkipData = skipData;
        }

        yield return server;
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(string animeId)
    {
        var id = Guid.Parse(animeId);
        var item = await client.Items[id].GetAsync();

        if (item is null)
        {
            yield break;
        }

        if (item.Type == BaseItemDto_Type.Movie)
        {
            yield return new Episode(this, animeId, animeId, 1);
        }
        else
        {
            var result = await GetChildItems(id);

            if (result is null)
            {
                yield break;
            }

            var seasons = result.Items?.Where(x => x.Type == BaseItemDto_Type.Season) ?? [];
            var episodeNumber = 0;
            foreach (var season in seasons)
            {
                if (season.Id is null)
                {
                    continue;
                }

                var episodes = await GetChildItems(season.Id.Value, ItemSortBy.IndexNumber);
                if (episodes is null)
                {
                    continue;
                }

                foreach (var ep in episodes.Items ?? [])
                {
                    yield return new Episode(this, animeId, $"{ep.Id}", ++episodeNumber);
                }
            }
        }
    }

    private async Task<BaseItemDtoQueryResult?> GetChildItems(Guid id, ItemSortBy sortBy = ItemSortBy.SortName)
    {
        return await client.Items.GetAsync(x =>
        {
            var query = x.QueryParameters;
            query.SortBy = [sortBy];
            query.SortOrder = [SortOrder.Ascending];
            query.Recursive = false;
            query.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated, ItemFields.Overview, ItemFields.Tags, ItemFields.Genres];
            query.ImageTypeLimit = 1;
            query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Banner, ImageType.Thumb];
            query.ParentId = id;
        });
    }

    private async Task<Uri?> GetMediaUrl(BaseItemDto dto)
    {
        if (dto.Id is not { } id)
        {
            return null;
        }

        if (dto.Type is not (BaseItemDto_Type.Episode or BaseItemDto_Type.Movie))
        {
            return null;
        }

        if (dto.MediaType is not BaseItemDto_MediaType.Video)
        {
            return null;
        }


        var startTime = TimeProvider.System.GetTimestamp();
        var playbackInfoDto = new PlaybackInfoDto
        {
            UserId = Settings.UserId,
            AutoOpenLiveStream = true,
            StartTimeTicks = startTime
        };

        // if (bitRate != 0)
        // {
        //     playbackInfoDto.MaxStreamingBitrate = bitRate;
        // }

        var playbackInfo = await client.Items[id].PlaybackInfo.PostAsync(playbackInfoDto);

        if (playbackInfo is null or { PlaySessionId: null } or { MediaSources: null })
        {
            return null;
        }

        SessionId = playbackInfo.PlaySessionId;
        var mediaSource = playbackInfo.MediaSources.FirstOrDefault(x => x.Id == id.ToString("N"));

        if (mediaSource is null)
        {
            return null;
        }

        MediaSourceId = mediaSource.Id;

        if (!string.IsNullOrEmpty(mediaSource.TranscodingUrl) && mediaSource.SupportsTranscoding == true)
        {
            return null;
        }

        if (mediaSource.SupportsDirectPlay == true)
        {
            return settings.Value.ServerUrl
                           .AppendPathSegment($"/Videos/{id}/stream")
                           .AppendQueryParam("container", mediaSource.Container)
                           .AppendQueryParam("playSessionId", SessionId)
                           .AppendQueryParam("startTimeTicks", startTime)
                           .AppendQueryParam("static", true)
                           .AppendQueryParam("tag", mediaSource.ETag)
                           .AppendQueryParam("ApiKey", Settings.AccessToken)
                           .ToUri();
        }

        return null;
    }
}