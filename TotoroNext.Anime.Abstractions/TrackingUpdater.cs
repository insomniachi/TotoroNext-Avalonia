using System.Reactive.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class TrackingUpdater(IFactory<ITrackingService, Guid> factory,
                             IMessenger messenger) : IHostedService, IRecipient<PlaybackState>
{
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

    public void Receive(PlaybackState message)
    {
        _ = ReceiveInternal(message);
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

        if (message.Anime.Tracking is null)
        {
            message.Anime.Tracking = new Tracking
            {
                Status = ListItemStatus.Watching,
                StartDate = DateTime.Now,
            };
        }

        message.Episode.IsCompleted = true;
        var tracking = message.Anime.Tracking;

        tracking.WatchedEpisodes = (int)message.Episode.Number;
        tracking.Status = message.Anime.TotalEpisodes == message.Episode.Number ? ListItemStatus.Completed : ListItemStatus.Watching;

        var tasks = factory.CreateAll()
                           .Select(service => new Tuple<ITrackingService, long?>(service, message.Anime.ExternalIds.GetId(service.ServiceName)))
                           .Where(x => x.Item2 is not null)
                           .Select(tuple => tuple.Item1.Update(tuple.Item2!.Value, tracking));

        messenger.Send(new TrackingUpdated
        {
            Anime = message.Anime,
            Episode = message.Episode
        });

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            //this.Log().Error("Unable to update tracking", ex);
        }
    }

    protected virtual long GetId(AnimeModel anime) => anime.Id;
}
