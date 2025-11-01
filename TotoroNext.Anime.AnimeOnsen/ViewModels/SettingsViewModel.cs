using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen.ViewModels;

public class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        SubtitleLanguage = settings.Value.SubtitleLanguage;
    }

    public string SubtitleLanguage
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.SubtitleLanguage = value);
    }

    public Dictionary<string, string> Languages { get; } = new()
    {
        ["English"] = "en-US",
        ["French"] = "fr-FR",
        ["Spanish"] = "es-LA",
        ["Portuguese (Brazil)"] = "pt-BR",
        ["Italian"] = "it-IT",
        ["German"] = "de-DE"
    };
}