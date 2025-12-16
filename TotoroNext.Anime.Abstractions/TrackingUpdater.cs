using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public sealed class TrackingUpdater(
    IFactory<ITrackingService, Guid> factory,
    IFactory<IMetadataService, Guid> metadataFactory,
    IAnimeMappingService animeMappingService,
    IMessenger messenger,
    ILogger<TrackingUpdater> logger) : IRecipient<PlaybackState>, ITrackingUpdater
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public void Receive(PlaybackState message)
    {
        _ = ReceiveInternal(message);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        messenger.Register(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        messenger.Unregister<PlaybackState>(this);
        return Task.CompletedTask;
    }

    public async Task UpdateTracking(AnimeModel anime, Tracking tracking)
    {
        foreach (var trackingService in factory.CreateAll())
        {
            var id = anime.ExternalIds.GetIdForService(trackingService.Name);

            if (id is not > 0)
            {
                id = animeMappingService.GetId(anime)?.GetIdForService(trackingService.Name);
            }

            if (id is not > 0)
            {
                id = await SearchId(anime, trackingService.Id);
            }


            if (id is null)
            {
                continue;
            }

            try
            {
                var response = await trackingService.Update(id.Value, tracking);
                if (anime.ServiceName == trackingService.Name)
                {
                    anime.Tracking = response;
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to update tracking");
            }
        }
    }

    private async Task<long?> SearchId(AnimeModel anime, Guid serviceId)
    {
        try
        {
            var metaDataService = metadataFactory.Create(serviceId);
            return metaDataService.Id == anime.ServiceId ? null : (await metaDataService.FindAnimeAsync(anime))?.Id;
        }
        catch
        {
            return null;
        }
    }

    private async Task ReceiveInternal(PlaybackState message)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (message.Duration == TimeSpan.Zero)
            {
                return;
            }

            if (message.Episode.Number < (message.Anime.Tracking?.WatchedEpisodes ?? 0))
            {
                return;
            }

            if (IsNotCompleted(message))
            {
                return;
            }

            if (message.Episode.IsCompleted)
            {
                return;
            }

            message.Episode.IsCompleted = true;
            
            message.Anime.Tracking ??= new Tracking
            {
                Status = ListItemStatus.Watching,
                StartDate = DateTime.Now
            };
            
            var tracking = message.Anime.Tracking;
            tracking.WatchedEpisodes = (int)message.Episode.Number;
            if (tracking.WatchedEpisodes == 1)
            {
                tracking.StartDate = DateTime.Now;
            }

            if (message.Anime.TotalEpisodes == tracking.WatchedEpisodes)
            {
                tracking.Status = ListItemStatus.Completed;
                tracking.FinishDate = DateTime.Now;
            }
            
            await UpdateTracking(message.Anime, tracking);

            messenger.Send(new TrackingUpdated
            {
                Anime = message.Anime,
                Episode = message.Episode
            });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to update tracking from playback state");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static bool IsNotCompleted(PlaybackState message)
    {
        var creditsDuration = message.Anime.MediaFormat is AnimeMediaFormat.Movie
            ? TimeSpan.FromMinutes(6)
            : TimeSpan.FromMinutes(2);

        return message.Duration - message.Position > creditsDuration;
    }
}