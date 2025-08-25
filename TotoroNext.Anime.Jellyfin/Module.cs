using Jellyfin.Sdk;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Jellyfin.ViewModels;
using TotoroNext.Anime.Jellyfin.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Jellyfin;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("13bd2ef6-5d5b-4bea-a3f5-d4b9c1391322"),
        Name = "Jellyfin",
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel)
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddSingleton<JellyfinSdkSettings>();
        services.AddSingleton<JellyfinApiClient>(sp =>
        {
            var settings = sp.GetRequiredService<JellyfinSdkSettings>();
            return new JellyfinApiClient(new JellyfinRequestAdapter(new JellyfinAuthenticationProvider(settings), settings));
        });
        services.AddTransient<IBackgroundInitializer, Initializer>();
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }

}

public class Settings
{
    public static Guid? UserId { get; set; }
    public static string? AccessToken { get; set; }
    
    public string? ServerUrl { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public Guid LibraryId { get; set; }
}