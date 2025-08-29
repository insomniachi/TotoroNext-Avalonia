using CommunityToolkit.Mvvm.Input;
using Microsoft.Playwright;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen.ViewModels;

public partial class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    private readonly DateTime? _renewTime;
    
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        Token = settings.Value.ApiToken?.Token ?? "";
        SubtitleLanguage = settings.Value.SubtitleLanguage;
        AutoUpdateApiToken = settings.Value.AutoUpdateApiToken;
        _renewTime = settings.Value.ApiToken?.RenewTime;
    }
    
    public string Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.ApiToken = new AnimeOnsenApiToken { Token = value, RenewTime = DateTime.Now.AddDays(5) });
    }

    public string SubtitleLanguage
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.SubtitleLanguage = value);
    }

    public bool AutoUpdateApiToken
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.AutoUpdateApiToken = value);
    }

    public Dictionary<string, string> Languages { get; } = new()
    {
        ["English"] = "en-US",
        ["French"] = "fr-FR",
        ["Spanish"] = "es-LA",
        ["Portuguese (Brazil)"] = "pt-BR",
        ["Italian"] = "it-IT",
        ["German"] = "de-DE",
    };
    
    [RelayCommand]
    public async Task UpdateApiToken()
    {
        var now = DateTime.Now;

        if (_renewTime is { } renewTime && now > renewTime)
        {
            return;
        }
        
        using var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Channel = "msedge",
            Headless = true
        });
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var tokenSource = new TaskCompletionSource<string>();
        
        page.Request += (_, request) =>
        {
            if (request.Url != "https://api.animeonsen.xyz/v4/content/index/recent/spotlight")
            {
                return;
            }

            if (!request.Headers.TryGetValue("authorization", out var authHeader) ||
                !authHeader.StartsWith("Bearer "))
            {
                return;
            }

            Token = authHeader["Bearer ".Length..];
            tokenSource.TrySetResult(Token);
        };

        await page.GotoAsync("https://www.animeonsen.xyz/");
        await tokenSource.Task;
    }
}