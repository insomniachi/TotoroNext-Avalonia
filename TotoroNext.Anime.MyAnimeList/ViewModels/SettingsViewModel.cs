using MalApi;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.MyAnimeList.ViewModels;

public partial class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        IncludeNsfw = settings.Value.IncludeNsfw;
        SearchLimit = settings.Value.SearchLimit;
    }

    public bool IncludeNsfw
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.IncludeNsfw = value);
    }

    public OAuthToken? Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Auth = value);
    }

    public double SearchLimit
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.SearchLimit = value);
    }
}
