using CommunityToolkit.Mvvm.Messaging;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Hosting;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Jellyfin;

public class JellyfinTrackingUpdater(
    IMessenger messenger,
    JellyfinApiClient client) : IHostedService,
                                IRecipient<PlaybackState>,
                                IRecipient<PlaybackEnded>
{
    private PlaybackProgressInfo? _info;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        messenger.Register<PlaybackState>(this);
        messenger.Register<PlaybackEnded>(this);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        messenger.Unregister<PlaybackState>(this);
        messenger.Unregister<PlaybackEnded>(this);
        return Task.CompletedTask;
    }

    public void Receive(PlaybackEnded message)
    {
        if (_info is null)
        {
            return;
        }

        var stopInfo = new PlaybackStopInfo
        {
            SessionId = _info.SessionId,
            MediaSourceId = _info.MediaSourceId,
            PositionTicks = _info.PositionTicks,
            ItemId = _info.ItemId
        };

        client.Sessions.Playing.Stopped.PostAsync(stopInfo).GetAwaiter().GetResult();

        _info = null;
    }

    public void Receive(PlaybackState message)
    {
        if (!message.Episode.IsFromProvider(typeof(AnimeProvider)))
        {
            return;
        }

        var id = Guid.Parse(message.Episode.Id);
        _info ??= new PlaybackProgressInfo();
        _info.ItemId = id;
        _info.PositionTicks = message.Position.Ticks;
        _info.MediaSourceId = AnimeProvider.MediaSourceId;
        _info.SessionId = AnimeProvider.SessionId;

        client.Sessions.Playing.Progress.PostAsync(_info).GetAwaiter().GetResult();
    }
}