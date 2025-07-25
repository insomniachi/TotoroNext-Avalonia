using System.Collections.Specialized;
using System.Text.Json.Serialization;
using Avalonia.Platform.Storage;
using Flurl.Http;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anilist.ViewModels;

public class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    private readonly OAuthListener _listener;

    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        _listener = new OAuthListener(3333, ProcessQuery);
        IncludeNsfw = settings.Value.IncludeNsfw;
        SearchLimit = settings.Value.SearchLimit;
        TitleLanguage = settings.Value.TitleLanguage;
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
        set => SetAndSaveProperty(ref field, value, x => x.TitleLanguage = value);
    }

    public TitleLanguage[] TitleLanguages { get; } = [TitleLanguage.English, TitleLanguage.Romaji];

    private async Task ProcessQuery(NameValueCollection arg)
    {
        var code = arg["code"]!;
        var token = await "https://anilist.co/api/v2/oauth/token"
                          .PostJsonAsync(new AuthTokenRequest(code)).ReceiveJson<AniListAuthToken>();
        token.CreatedAt = DateTime.Now;
        Token = token;
    }

    public async Task Login(ILauncher launcher)
    {
        _listener.Start();
        await
            launcher.LaunchUriAsync(new
                                        Uri($"https://anilist.co/api/v2/oauth/authorize?client_id={Settings.ClientId}&redirect_uri={Settings.RedirectUrl}&response_type=code"));
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