using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Flurl;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.Anilist.ViewModels;

public class SettingsViewModel : ModuleSettingsViewModel<Settings>, IInitializable
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        IncludeNsfw = settings.Value.IncludeNsfw;
        SearchLimit = settings.Value.SearchLimit;
        TitleLanguage = settings.Value.TitleLanguage;
        Token = settings.Value.Token;
    }

    public bool IncludeNsfw
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.IncludeNsfw = value);
    }

    public string? Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.Token = value);
    }

    public double SearchLimit
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.SearchLimit = value);
    }

    public TitleLanguage TitleLanguage
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.TitleLanguage = value);
    }

    public async Task Login(IToastManager toastManager)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var options =
            new WebAuthenticatorOptions(new Uri($"https://anilist.co/api/v2/oauth/authorize?client_id={Settings.ClientId}&response_type=token"),
                                        new Uri("https://anilist.co/api/v2/oauth/pin"))
            {
                PreferNativeWebDialog = true
            };

        var result = await WebAuthenticationBroker.AuthenticateAsync(desktop.MainWindow!, options);
        var query = new Url("https://dummy.com/?" + result.CallbackUri.Fragment.TrimStart('#')).QueryParams;

        var accessToken = query.FirstOrDefault(p => p.Name == "access_token").Value?.ToString();

        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        Token = accessToken;
        toastManager.Show(new Toast
        {
            Content = "Anilist Authenticated",
            Expiration = TimeSpan.FromSeconds(2),
            Type = Avalonia.Controls.Notifications.NotificationType.Success
        });
    }
}