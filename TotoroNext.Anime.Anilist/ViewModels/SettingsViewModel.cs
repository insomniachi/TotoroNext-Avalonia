using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anilist.ViewModels;

public class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        IncludeNsfw = settings.Value.IncludeNsfw;
        SearchLimit = settings.Value.SearchLimit;
        TitleLanguage = settings.Value.TitleLangauge;
    }

    public bool IncludeNsfw
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.IncludeNsfw = value);
    }

    public AniListAuthToken? Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Auth = value);
    }

    public double SearchLimit
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.SearchLimit = value);
    }

    public TitleLanguage TitleLanguage
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.TitleLangauge = value);
    }

    public TitleLanguage[] TitleLanguages { get; } = [TitleLanguage.English, TitleLanguage.Romaji];
}