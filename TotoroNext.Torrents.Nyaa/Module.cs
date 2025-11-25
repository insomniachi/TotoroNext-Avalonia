using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Torrents.Nyaa;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("6df72a21-3130-4975-bc49-1d8982d96c35"),
        Name = "Nyaa",
        Description = "Nyaa torrent indexer module",
    };
    
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<ITorrentIndexer, Indexer>(Guid.Empty);
    }
}

public class Settings : OverridableConfig 
{
    public string ReleaseGroup { get; set; } = "";
    public string Quality { get; set; } = "1080";
}