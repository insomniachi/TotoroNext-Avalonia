using Microsoft.Extensions.Hosting;

namespace TotoroNext.Anime.Abstractions;

public interface IPlaybackProgressService : IHostedService
{
    Dictionary<float, ProgressInfo> GetProgress(long id);
}
