using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.LocalStorage;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Name = "Local",
        Id = new Guid("a294557f-4f1b-4c0f-a37d-39825019a761"),
        Components = [ComponentTypes.AnimeProvider]
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}