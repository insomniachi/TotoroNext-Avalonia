using Microsoft.Extensions.Hosting;

namespace TotoroNext.Anime.Abstractions;

public interface ITrackingUpdater : IHostedService
{
    Task UpdateTracking(AnimeModel anime, Tracking tracking);
}