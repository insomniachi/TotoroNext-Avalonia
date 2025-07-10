using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AllAnime;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("489576c5-2879-493b-874a-7eb14e081280"),
        Name = "AllAnime",
        Description = "AllAnime's goal is to provide you with the highest possible amount of daily anime episodes/manga chapters for free and without any kind of limitation.",
        HeroImage = ResourceHelper.GetResource("hero.png"),
        Components = [ComponentTypes.AnimeProvider]
    };

    public void RegisterComponents(IComponentRegistry components)
    {
        components.RegisterComponent(ComponentTypes.AnimeProvider, Descriptor);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}
