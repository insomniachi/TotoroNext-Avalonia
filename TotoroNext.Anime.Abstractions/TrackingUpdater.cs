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
        var trackingServices = factory.CreateAll();
        foreach (var trackingService in trackingServices)
        {
            if (trackingService.Name == nameof(ExternalIds.Anilist))
            {
                continue;
            }

            var id = anime.ExternalIds.GetId(trackingService.Name);
            if (id is null)
            {
                try
                {
                    var metaDataService = metadataFactory.Create(trackingService.Id);
                    if (metaDataService.Id == anime.ServiceId)
                    {
                        continue;
                    }

                    id = (await metaDataService.FindAnimeAsync(anime))?.Id;
                }
                catch
                {
                    continue;
                }
            }

            if (id is null)
            {
                continue;
            }

            await trackingService.Update(id.Value, tracking);
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