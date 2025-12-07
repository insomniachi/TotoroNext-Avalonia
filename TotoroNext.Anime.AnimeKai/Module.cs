using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeKai;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Name = "Anime Kai",
        Id = new Guid("d3b5f4e2-4c6e-4f3a-9f7e-2b8e5c6d7a1b"),
        Components = [ComponentTypes.AnimeProvider],
        HeroImage = ResourceHelper.GetResource("animekai.jpg"),
        Description = "Largest anime library, Old series in good quality."
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddHttpClient(typeof(Module).FullName!, client => { client.BaseAddress = new Uri("https://animekai.to/"); });
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}