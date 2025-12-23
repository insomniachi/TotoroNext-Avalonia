using Microsoft.Extensions.Hosting;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface ITrackingUpdater : IHostedService
{
    Task UpdateTracking(AnimeModel anime, Tracking tracking);
    Task RemoveTracking(AnimeModel anime);
}