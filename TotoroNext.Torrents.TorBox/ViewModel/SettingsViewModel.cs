using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Torrents.TorBox.ViewModel;

public class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        Token = settings.Value.Token;
    }

    public string Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Token = value);
    }
}