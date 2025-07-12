using System.Diagnostics;
using TotoroNext.MediaEngine.Abstractions;

namespace TotoroNext.Anime.Aniskip;

internal class MediaSegmentsProvider(IAniskipClient client) : IMediaSegmentsProvider
{
    public async Task<List<MediaSegment>> GetSegments(long id, float episode, double mediaLength)
    {
        try
        {
            var result = await client.GetSkipTimes(id, episode, new GetSkipTimesQueryV2
            {
                EpisodeLength = mediaLength,
                Types = [SkipType.Recap, SkipType.Opening, SkipType.Ending]
            });

            if (!result.IsFound)
            {
                return [];
            }

            var segments = result.Results
                                 .OrderBy(x => x.Interval.StartTime)
                                 .Select(CreateMediaSegment).ToList();

            return [.. segments.MakeContiguousSegments(TimeSpan.FromSeconds(mediaLength))];
        }
        catch
        {
            return [];
        }
    }

    private static MediaSegment CreateMediaSegment(SkipTime skipTime)
    {
        return new MediaSegment(
                                ConvertType(skipTime.SkipType),
                                TimeSpan.FromSeconds(skipTime.Interval.StartTime),
                                TimeSpan.FromSeconds(skipTime.Interval.EndTime)
                               );
    }

    private static MediaSectionType ConvertType(SkipType skipType)
    {
        return skipType switch
        {
            SkipType.Recap => MediaSectionType.Recap,
            SkipType.Opening => MediaSectionType.Opening,
            SkipType.Ending => MediaSectionType.Ending,
            SkipType.MixedOpening => throw new NotSupportedException(),
            SkipType.MixedEnding => throw new NotSupportedException(),
            _ => throw new UnreachableException()
        };
    }
}