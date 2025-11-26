using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TokyoInsider;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Name = "Tokyo Insider",
        Id = new Guid("8caaa814-2abf-43db-8f92-ad1c56bd6a12"),
        Components = [ComponentTypes.AnimeProvider, ComponentTypes.AnimeDownloader],
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddTransient<IInitializer, Initializer>();
        services.AddTransient<IBackgroundInitializer, Initializer>();
        // services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}