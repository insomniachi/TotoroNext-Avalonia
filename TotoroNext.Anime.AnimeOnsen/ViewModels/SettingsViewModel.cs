using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen.ViewModels;

public class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        Token = settings.Value.ApiToken;
    }
    
    public string Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.ApiToken = value);
    }
}