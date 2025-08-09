using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeParadise;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("3b61e01b-dd7c-4492-a564-4ee031959097"),
        Name = "Anime Paradise",
        Description ="",
        Components = [ComponentTypes.AnimeProvider]
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
}

public class Settings
{
    public static List<string> SubtitleLanguages { get; } =
    [
        "English",
    ];
    
    public string SubtitleLanguage { get; set; } = "English";
}