using CommunityToolkit.Mvvm.Input;
using Microsoft.Playwright;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen.ViewModels;

public partial class SettingsViewModel : ModuleSettingsViewModel<Settings>
{
    public SettingsViewModel(IModuleSettings<Settings> settings) : base(settings)
    {
        Token = settings.Value.ApiToken;
        SubtitleLanguage = settings.Value.SubtitleLanguage;
    }
    
    public string Token
    {
        get;
        set => SetAndSaveProperty(ref field, value, x => x.ApiToken = value);
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
        ["German"] = "de-DE",
    };
    
    [RelayCommand]
    public async Task UpdateApiToken()
    {
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