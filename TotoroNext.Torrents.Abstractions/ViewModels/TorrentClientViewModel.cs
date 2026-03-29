using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using ReactiveUI;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Torrents.Abstractions.ViewModels;

[UsedImplicitly]
public sealed class TorrentClientViewModel(IFactory<ITorrentClient, Guid> factory) : ObservableObject, IAsyncInitializable, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Lock _torrentLock = new();

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
                var tcs = new TaskCompletionSource();
                
                lock (_torrentLock)
                {
                    if(Torrents.FirstOrDefault(x => x.Hash == torrent.Hash) is { } existing)
                    {
                        RxApp.MainThreadScheduler.Schedule(() => 
                        {
                            existing.Update(torrent);
                            tcs.SetResult();
                        });
                    }
                    else
                    {
                        RxApp.MainThreadScheduler.Schedule(() => 
                        {
                            Torrents.Add(torrent);
                            tcs.SetResult();
                        });
                    }
                }
                
                await tcs.Task;
            }

        }, _cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
    }
}