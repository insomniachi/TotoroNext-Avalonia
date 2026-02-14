using TotoroNext.Anime.Abstractions;

namespace TotoroNext.Anime.SubsPlease;

public class AnimeScheduleProvider : IAnimeScheduleProvider
{
    public Task<DateTimeOffset?> GetNextEpisodeAiringTime(string animeId, CancellationToken ct)
    {
        return Schedule.Items.FirstOrDefault(x => x.Id == animeId) is not { } result
            ? Task.FromResult<DateTimeOffset?>(null) 
            : Task.FromResult<DateTimeOffset?>(result.AirsAt);
    }
}