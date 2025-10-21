using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using TotoroNext.Module;
using Velopack;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public partial class DownloadUpdateViewModel(
    UpdateManager updateManager,
    UpdateInfo update,
    ILogger<DownloadUpdateViewModel> logger) : DialogViewModel, IAsyncInitializable
{
    private readonly CancellationTokenSource _cts = new();

    [ObservableProperty] public partial int Progress { get; set; }

    public async Task InitializeAsync()
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Downloading Update");
        }

        await updateManager.DownloadUpdatesAsync(update, UpdateProgress, _cts.Token);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Downloading Update complete");
        }

        updateManager.ApplyUpdatesAndRestart(update);

        Close();
    }

    [RelayCommand]
    private async Task CancelUpdate()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        Close();
    }

    private void UpdateProgress(int progress)
    {
        RxApp.MainThreadScheduler.Schedule(() => Progress = progress);
    }
}