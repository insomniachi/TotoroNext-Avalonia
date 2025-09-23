using CommunityToolkit.Mvvm.Messaging;
using DiscordRPC;
using Microsoft.Extensions.Hosting;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Discord;

internal class RpcService(
    IModuleSettings<Settings> settings,
    IAnimeExtensionService extensionService,
    IMessenger messenger) : IHostedService,
                            IRecipient<PlaybackState>,
                            IRecipient<PlaybackEnded>,
                            IRecipient<SongPlaybackState>
{
    private readonly DiscordRpcClient _client = new("997177919052984622");
    private readonly bool _isEnabled = settings.Value.IsEnabled;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        messenger.Register<PlaybackState>(this);
        messenger.Register<PlaybackEnded>(this);
        messenger.Register<SongPlaybackState>(this);

        return Task.Run(async () =>
        {
            bool isInitialized;
            do
            {
                isInitialized = _client.Initialize();
                await Task.Delay(1000, cancellationToken);
            } while (!isInitialized);
        }, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client.IsInitialized)
        {
            _client.Deinitialize();
        }

        messenger.Unregister<PlaybackState>(this);
        messenger.Unregister<PlaybackEnded>(this);
        messenger.Unregister<SongPlaybackState>(this);
        return Task.CompletedTask;
    }
    
    public void Receive(PlaybackEnded message)
    {
        _client.ClearPresence();
    }

    public void Receive(PlaybackState message)
    {
        if (!_client.IsInitialized)
        {
            return;
        }

        if (!_isEnabled)
        {
            return;
        }

        if (!extensionService.IsInIncognitoMode(message.Anime.Id))
        {
            return;
        }

        var now = DateTime.UtcNow;
        _client.Update(p =>
        {
            p.Type = ActivityType.Watching;
            p.Details = message.Anime.Title;
            p.State = $"Episode {message.Episode.Number}";
            p.Assets ??= new Assets();
            p.Assets.LargeImageKey = message.Anime.Image;
            p.Timestamps = new Timestamps
            {
                Start = now - message.Position,
                End = now + (message.Duration - message.Position)
            };
            p.Buttons =
            [
                new Button
                {
                    Label = message.Anime.ServiceName,
                    Url = message.Anime.Url
                }
            ];
        });
    }

    public void Receive(SongPlaybackState message)
    {
        var now = DateTime.UtcNow;

        _client.Update(p =>
        {
            p.Type = ActivityType.Listening;
            p.Details = message.Song.SongName;
            p.State = $"{message.Anime.Title} - {message.Song.Slug}";
            p.Assets ??= new Assets();
            p.Assets.LargeImageKey = message.Anime.Image;
            p.Timestamps = new Timestamps
            {
                Start = now - message.Position,
                End = now + (message.Duration - message.Position)
            };
            p.Buttons =
            [
                new Button
                {
                    Label = message.Anime.ServiceName,
                    Url = message.Anime.Url
                }
            ];
        });
    }
}