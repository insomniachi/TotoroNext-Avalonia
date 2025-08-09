using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anizone;

public class Module : IModule
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("5bdd85ad-523a-48b0-9af6-e84f46e565f6"),
        Name = "Anizone",
        Components = [ComponentTypes.AnimeProvider]
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}