using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions.Models;
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

    public Guid SelectedTorrentService
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

    public Guid SelectedTorrentIndexer
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

    public Guid SelectedTorrentClient
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

    public string? HomeView
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    } = "Home";

    public string? YtdlpPath
    {
        get;
        set => SetAndSaveProperty(ref field, value);
    }

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

            var fallback = prop.GetValue(this);
            var genericMethod = readSettingMethod.MakeGenericMethod(propertyType);
            var value = genericMethod.Invoke(_localSettingsService, [prop.Name, fallback]);
            
            prop.SetValue(this, value);
        }
    }
}