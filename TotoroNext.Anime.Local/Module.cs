using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.ViewModels;
using TotoroNext.Anime.Abstractions.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Local;

public class Module : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddKeyedViewMap<UpdateTrackingView, UpdateTrackingViewModel>($"tracking/Local");
        
        services.AddSingleton<ILiteDbContext, LiteDbContext>();
        
        services.AddTransient<IAnimeMappingService, AnimeMappingService>();
        services.AddTransient<IBackgroundInitializer, Initializer>();
        
        services.AddKeyedTransient<IMetadataService, MetadataService>(Guid.Empty);
        services.AddKeyedTransient<ITrackingService, TrackingService>(Guid.Empty);
        
        services.AddHostedService(sp => sp.GetRequiredService<ILiteDbContext>());
    }
}