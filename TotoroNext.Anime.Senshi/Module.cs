using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Senshi;

public class Module : IModule
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("c6066fda-8e30-4545-9599-8e6c3e917312"),
        Name = "Senshi",
        Components = [ComponentTypes.AnimeProvider],
        HeroImage = ResourceHelper.GetResource("senshi.jpg")
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        services.AddHttpClient("Senshi", client =>
        {
            client.BaseAddress = new Uri("https://senshi.live/");
        });
    }
}