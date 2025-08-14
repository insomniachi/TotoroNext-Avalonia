using JetBrains.Annotations;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AllAnime.ViewModels;

[UsedImplicitly]
public class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        TranslationType = settings.Value.TranslationType;
    }

    public TranslationType TranslationType
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.TranslationType = value);
    }
}