using System.Text.Json.Serialization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Flurl.Http;
using TotoroNext.Anime.Anilist.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.Anilist.ViewModels;

public class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        IncludeNsfw = settings.Value.IncludeNsfw;
        SearchLimit = settings.Value.SearchLimit;
        TitleLanguage = settings.Value.TitleLanguage;
        Auth = settings.Value.Auth;
    }

    public bool IncludeNsfw
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.IncludeNsfw = value);
    }

    public AniListAuthToken? Auth
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
        set => SetAndSaveProperty(ref field, value, x => x.TitleLanguage = value);
    }

    public TitleLanguage[] TitleLanguages { get; } = [TitleLanguage.English, TitleLanguage.Romaji];

    public async Task Login(ILauncher launcher, IToastManager toastManager)
    {
        // await launcher
        //     .LaunchUriAsync(new
        //                         Uri($"https://anilist.co/api/v2/oauth/authorize?client_id={Settings.ClientId}&redirect_uri={Settings.RedirectUrl}&response_type=code"));

        var options = new DialogOptions
        {
            Title = "Copy & Paste the text from the browser",
            Mode = DialogMode.Info,
            Button = DialogButton.OKCancel,
            CanDragMove = false,
            IsCloseButtonVisible = false,
            CanResize = false,
            ShowInTaskBar = false,
            StartupLocation = WindowStartupLocation.CenterOwner
        };

        var vm = new GetAnilistCodeDialogViewModel();
        var result = await Dialog.ShowModal<GetAnilistCodeDialog, GetAnilistCodeDialogViewModel>(vm, options: options);

        if (result == DialogResult.OK)
        {
            //Auth = await GetAuthToken(vm.Code);
            
            toastManager.Show(new Toast()
            {
                Content = "Anilist Authenticated",
                Expiration = TimeSpan.FromSeconds(2),
                Type = Avalonia.Controls.Notifications.NotificationType.Success
            });
        }
    }

    private static async Task<AniListAuthToken> GetAuthToken(string code)
    {
        var token = await "https://anilist.co/api/v2/oauth/token"
                          .PostJsonAsync(new AuthTokenRequest(code)).ReceiveJson<AniListAuthToken>();
        token.CreatedAt = DateTime.Now;
        return token;
    }
}

internal class AuthTokenRequest(string code)
{
    [JsonPropertyName("grant_type")] public string GrantType { get; } = "authorization_code";

    [JsonPropertyName("client_id")] public string ClientId { get; } = Settings.ClientId.ToString();

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; } = Cyrpto.Decrypt("QVpa7tfm4MlTDB7DZyWCvOlskzZNNanGzt1brOJdZrejKUTh5VFPLIOm5h34XWyE");

    [JsonPropertyName("redirect_uri")] public string RedirectUrl { get; } = Settings.RedirectUrl;

    [JsonPropertyName("code")] public string Code { get; } = code;
}