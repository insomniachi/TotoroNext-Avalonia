using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

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
public partial class SettingsViewModel : ObservableObject, IInitializable
{
    private readonly SettingsModel _settings;

    public SettingsViewModel(IEnumerable<Descriptor> modules,
                             SettingsModel settings)
    {
        _settings = settings;
        var allModules = modules.ToList();

        MediaEngines = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.MediaEngine))];
        AnimeProviders = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.AnimeProvider))];
        TrackingServices = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.Tracking))];
        SegmentProviders = [.. allModules.Where(x => x.Components.Contains(ComponentTypes.MediaSegments))];
        DebridServices = [Descriptor.None, .. allModules.Where(x => x.Components.Contains(ComponentTypes.Debrid))];
    }

    public List<Descriptor> MediaEngines { get; }
    public List<Descriptor> AnimeProviders { get; }
    public List<Descriptor> TrackingServices { get; }
    public List<Descriptor> SegmentProviders { get; }
    public List<Descriptor> DebridServices { get; }


    [ObservableProperty] public partial SettingsModel? Settings { get; private set; }

    public void Initialize()
    {
        Settings = _settings;
    }
}