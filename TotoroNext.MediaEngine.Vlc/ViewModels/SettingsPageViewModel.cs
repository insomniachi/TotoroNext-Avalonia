using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Vlc.ViewModels;

public partial class SettingsPageViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsPageViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        Command = Settings.FileName;
        LaunchFullScreen = Settings.LaunchFullScreen;
    }

    public string? Command
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.FileName = value ?? "");
    }

    public bool LaunchFullScreen
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.LaunchFullScreen = value);
    }
}