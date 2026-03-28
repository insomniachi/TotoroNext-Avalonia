using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Torrents.Abstractions.ViewModels;

[UsedImplicitly]
public class TorrentClientViewModel(IFactory<ITorrentClient, Guid> factory) : ObservableObject, IAsyncInitializable
{
    private readonly CancellationTokenSource _cts = new();

    public ObservableCollection<TorrentViewModel> Torrents { get; } = [];
    
    public Task InitializeAsync()
    {
        var client = factory.CreateDefault();
        if (client is null)
        {
            return Task.CompletedTask;
        }
        
        return Task.Run(async () =>
        {
            await foreach (var torrent in client.GetTorrents(_cts.Token))
            {
                if(Torrents.FirstOrDefault(x => x.Hash == torrent.Hash) is { } existing)
                {
                    existing.Update(torrent);
                }
                else
                {
                    RxApp.MainThreadScheduler.Schedule(() => Torrents.Add(torrent));
                }
            }

        }, _cts.Token);
    }
}