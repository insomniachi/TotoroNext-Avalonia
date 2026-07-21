using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;
using Ursa.Controls;
using Velopack;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public partial class SettingsViewModel : ObservableObject, IInitializable, IInitializer
{
    private readonly IFactory<ITrackingService, Guid> _trackingServiceFactory;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly IMessenger _messenger;
    private readonly UpdateManager _updateManager;

    public SettingsViewModel(IEnumerable<Descriptor> modules,
                             IFactory<ITrackingService, Guid> trackingServiceFactory,
                             IDialogService dialogService,
                             IMessenger messenger,
                             UpdateManager updateManager,
                             SettingsModel settings,
                             ILogger<SettingsViewModel> logger)
    {
        _trackingServiceFactory = trackingServiceFactory;
        _dialogService = dialogService;
        _messenger = messenger;
        _updateManager = updateManager;
        _logger = logger;
        Settings = settings;
        var allModules = modules.ToList();

        MediaEngines = [Descriptor.Default, .. allModules.Where(x => x.Components.Contains(ComponentTypes.MediaEngine))];
        AnimeProviders = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];
        TrackingServices = [Descriptor.Default, .. allModules.Where(x => x.Components.Contains(ComponentTypes.Tracking))];
        SegmentProviders = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.MediaSegments))];
        TorrentStreamServices = [Descriptor.None, Descriptor.New("Local", MonoTorrentStream.MonoTorrentStreamId), .. allModules.Where(x => x.Components.Contains(ComponentTypes.Debrid))];
        TorrentClients = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.TorrentClient))];
        TorrentIndexers = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.TorrentIndexer))];
    }

    public List<Descriptor> MediaEngines { get; }
    public List<Descriptor> AnimeProviders { get; }
    public List<Descriptor> TrackingServices { get; }
    public List<Descriptor> SegmentProviders { get; }
    public List<Descriptor> TorrentStreamServices { get; }
    public List<Descriptor> TorrentClients { get; }
    public List<Descriptor> TorrentIndexers { get; }
    public List<string> HomeViews { get; } = ["Home", "Anime List"];
    public Version? CurrentVersion => Assembly.GetEntryAssembly()?.GetName().Version;

    public ObservableCollection<string> Themes { get; } =
    [
        "Default",
        "Light",
        "Dark",
        "Aquatic",
        "Desert",
        "Dusk",
        "NightSky"
    ];

    public SettingsModel Settings { get; }

    public void Initialize()
    {
        if (Settings.SelectedAnimeProvider.GetValueOrDefault() == Guid.Empty)
        {
            Settings.SelectedAnimeProvider = AnimeProviders.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        if (Settings.SelectedTrackingService.GetValueOrDefault() == Guid.Empty)
        {
            Settings.SelectedTrackingService = TrackingServices.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        if (Settings.SelectedSegmentsProvider.GetValueOrDefault() == Guid.Empty)
        {
            Settings.SelectedSegmentsProvider = SegmentProviders.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        if (Settings.SelectedTorrentIndexer.GetValueOrDefault() == Guid.Empty)
        {
            Settings.SelectedTorrentIndexer = TorrentIndexers.FirstOrDefault()?.Id ?? Guid.Empty;
        }
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo is null)
            {
                await _dialogService.Information("You are running the latest version.");
                return;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Update found: {info}", JsonSerializer.Serialize(updateInfo));
            }

            var answer = await _dialogService.Question("Update found", $"Download and install {updateInfo.TargetFullRelease.Version}?");

            if (answer == MessageBoxResult.Yes)
            {
                _messenger.Send(new NavigateToViewModelDialogMessage
                {
                    Button = DialogButton.None,
                    CloseButtonVisible = false,
                    Title = "Downloading Update",
                    ViewModel = typeof(DownloadUpdateViewModel),
                    Data = updateInfo
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    [RelayCommand]
    private async Task SyncList()
    {
        if (Settings.SelectedTrackingService == Guid.Empty)
        {
            return;
        }

        if (_trackingServiceFactory.Create(Guid.Empty) is not ILocalTrackingService lts)
        {
            return;
        }

        var service = _trackingServiceFactory.Create(Settings.SelectedTrackingService.GetValueOrDefault());
        if (service is null)
        {
            return;
        }

        var userlist = await service.GetUserList(CancellationToken.None);
        await Task.Run(() => lts.SyncList(userlist));
    }
}