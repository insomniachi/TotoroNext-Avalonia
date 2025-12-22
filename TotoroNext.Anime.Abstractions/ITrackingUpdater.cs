using Microsoft.Extensions.Hosting;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public interface ITrackingUpdater : IHostedService
{
    Task UpdateTracking(Models.AnimeModel anime, Tracking tracking);
}