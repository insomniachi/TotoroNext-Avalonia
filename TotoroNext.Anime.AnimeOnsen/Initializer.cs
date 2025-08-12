using System.Text.RegularExpressions;
using Flurl.Http;
using TotoroNext.Anime.AnimeOnsen.ViewModels;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen;

public sealed partial class Initializer(SettingsViewModel vm) : IInitializer
{
    public async Task InitializeAsync()
    {
        await vm.UpdateToken();
        await GetSearchToken();
    }
    
    private static async Task GetSearchToken()
    {
        var content = await "https://www.animeonsen.xyz/".GetStringAsync();
        var match = GetTokenRegex().Match(content);

        if (match.Success)
        {
            Settings.SearchTokenTaskCompletionSource.SetResult(match.Groups["Token"].Value);
        }
        else
        {
            Settings.SearchTokenTaskCompletionSource.SetException(new Exception("Token not found"));
        }
    }
    
    
    [GeneratedRegex("""
                    <meta name="ao-search-token" content="(?<Token>.*)"
                    """)]
    private static partial Regex GetTokenRegex();
}