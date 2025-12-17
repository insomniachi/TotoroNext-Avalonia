using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;
using Velopack;

namespace TotoroNext.ViewModels;

[UsedImplicitly]
public class SettingsModel : ObservableObject
{
    private readonly ILocalSettingsService _localSettingsService;

    public SettingsModel(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
        Initialize();
    }

    public Guid SelectedMediaEngine
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

    public Guid SelectedAnimeProvider
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

    public Guid SelectedTrackingService
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

    public Guid SelectedSegmentsProvider
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

    public Guid SelectedDebridService
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

    public Guid SelectedTorrentIndexer
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

    public SkipMethod OpeningSkipMethod
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    } = SkipMethod.Ask;

    public SkipMethod EndingSkipMethod
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    } = SkipMethod.Ask;

    public string SelectedTheme
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    } = "Dark";

    public bool AutoUpdate
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    } = true;

    protected void SetAndSaveProperty<TProperty>(ref TProperty field, TProperty value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<TProperty>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        _localSettingsService.SaveSetting(propertyName, value);
        OnPropertyChanged(propertyName);
    }

    public void Initialize()
    {
        var readSettingMethod = typeof(ILocalSettingsService)
                                .GetMethods()
                                .First(m => m is { Name: nameof(ILocalSettingsService.ReadSetting), IsGenericMethod: true });

        foreach (var prop in GetType().GetProperties().Where(x => x.CanWrite))
        {
            var propertyType = prop.PropertyType;

            var fallback = propertyType.IsValueType
                ? Activator.CreateInstance(propertyType)
                : null;

            var genericMethod = readSettingMethod.MakeGenericMethod(propertyType);
            var value = genericMethod.Invoke(_localSettingsService, [prop.Name, fallback]);
            prop.SetValue(this, value);
        }
    }
}

[UsedImplicitly]
public partial class SettingsViewModel : ObservableObject, IInitializable, IInitializer
{
    private readonly IFactory<ITrackingService, Guid> _trackingServiceFactory;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SettingsViewModel> _logger;
    private readonly IMessenger _messenger;
    private readonly SettingsModel _settings;
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
        _settings = settings;
        _logger = logger;
        var allModules = modules.ToList();

        MediaEngines = [Descriptor.Default, .. allModules.Where(x => x.Components.Contains(ComponentTypes.MediaEngine))];
        AnimeProviders = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];
        TrackingServices = [Descriptor.Default, .. allModules.Where(x => x.Components.Contains(ComponentTypes.Tracking))];
        SegmentProviders = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.MediaSegments))];
        DebridServices = [Descriptor.None, .. allModules.Where(x => x.Components.Contains(ComponentTypes.Debrid))];
    }

    public List<Descriptor> MediaEngines { get; }
    public List<Descriptor> AnimeProviders { get; }
    public List<Descriptor> TrackingServices { get; }
    public List<Descriptor> SegmentProviders { get; }
    public List<Descriptor> DebridServices { get; }
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

    [ObservableProperty] public partial SettingsModel? Settings { get; private set; }

    public void Initialize()
    {
        Settings = _settings;
        if (Settings.SelectedAnimeProvider == Guid.Empty)
        {
            Settings.SelectedAnimeProvider = AnimeProviders.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        if (Settings.SelectedTrackingService == Guid.Empty)
        {
            _settings.SelectedTrackingService = TrackingServices.FirstOrDefault()?.Id ?? Guid.Empty;
        }

        if (Settings.SelectedSegmentsProvider == Guid.Empty)
        {
            Settings.SelectedSegmentsProvider = SegmentProviders.FirstOrDefault()?.Id ?? Guid.Empty;
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
        if (_settings.SelectedTrackingService == Guid.Empty)
        {
            return;
        }

        if (_trackingServiceFactory.Create(Guid.Empty) is not ILocalTrackingService lts)
        {
            return;
        }

        var service = _trackingServiceFactory.Create(_settings.SelectedTrackingService);
        var userlist = await service.GetUserList(CancellationToken.None);
        await Task.Run(() => lts.SyncList(userlist));
    }
}