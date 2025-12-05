using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Local;

public class Module : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IBackgroundInitializer, Initializer>();
        services.AddKeyedTransient<IMetadataService, MetadataService>(Guid.Empty);
        services.AddKeyedTransient<ITrackingService, TrackingService>(Guid.Empty);
    }
}