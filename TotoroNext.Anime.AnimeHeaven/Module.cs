using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeHeaven;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("3b61e01b-dd7c-4492-a564-4ee031959097"),
        Name = "AnimeHeaven",
        Description ="",
        Components = [ComponentTypes.AnimeProvider]
    };
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        services.AddHttpClient("AnimeHeaven", client =>
        {
            client.BaseAddress = new Uri("https://www.animeheaven.me/");
        });
    }
}