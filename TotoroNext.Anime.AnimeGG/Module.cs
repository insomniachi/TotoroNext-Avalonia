using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeGG;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Name = "Anime GG",
        Id = new Guid("9cc96db6-27b2-41a7-b0ae-c2cb1c8a6dc4"),
        Description = "Anime provider for Anime GG",
        Components = [ComponentTypes.AnimeProvider]
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}