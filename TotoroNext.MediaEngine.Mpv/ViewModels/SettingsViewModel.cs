using JetBrains.Annotations;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Mpv.ViewModels;

[UsedImplicitly]
public sealed class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
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