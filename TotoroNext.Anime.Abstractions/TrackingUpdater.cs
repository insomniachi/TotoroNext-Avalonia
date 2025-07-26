using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public sealed class TrackingUpdater(
    IFactory<ITrackingService, Guid> factory,
    IFactory<IMetadataService, Guid> metadataFactory,
    IMessenger messenger) : IRecipient<PlaybackState>, ITrackingUpdater
{
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
            var id = anime.ExternalIds.GetId(trackingService.Name) ?? await SearchId(anime, trackingService.Id);

            if (id is null)
            {
                continue;
            }

            await trackingService.Update(id.Value, tracking);
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
        if (message.Duration == TimeSpan.Zero)
        {
            return;
        }

        if (message.Episode.Number < (message.Anime.Tracking?.WatchedEpisodes ?? 0))
        {
            return;
        }

        if (message.Duration - message.Position > TimeSpan.FromMinutes(2))
        {
            return;
        }

        if (message.Episode.IsCompleted)
        {
            return;
        }

        message.Anime.Tracking ??= new Tracking
        {
            Status = ListItemStatus.Watching,
            StartDate = DateTime.Now
        };

        message.Episode.IsCompleted = true;
        var tracking = message.Anime.Tracking;

        tracking.WatchedEpisodes = (int)message.Episode.Number;
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        tracking.Status = message.Anime.TotalEpisodes == message.Episode.Number ? ListItemStatus.Completed : ListItemStatus.Watching;

        await UpdateTracking(message.Anime, tracking);

        messenger.Send(new TrackingUpdated
        {
            Anime = message.Anime,
            Episode = message.Episode
        });
    }
}