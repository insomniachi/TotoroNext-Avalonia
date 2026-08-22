using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.ViewModels;
using TotoroNext.Anime.Abstractions.Views;
using TotoroNext.Anime.Local.ViewModels;
using TotoroNext.Anime.Local.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Local;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new Descriptor
    {
        Id = new Guid("5500de7e-4268-4edf-afcd-d445fec437e1"),
        Name = "Offline Anime Database",
        Components = [ComponentTypes.Metadata, ComponentTypes.Tracking],
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("oad.png")
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedViewMap<UpdateTrackingView, UpdateTrackingViewModel>("tracking/Local");
        services.AddViewMap<SettingsView, SettingsViewModel>();

        services.AddSingleton<IDbContext, DbContext>();
        services.AddTransient<IAnimeMappingService, AnimeMappingService>();

        services.AddKeyedTransient<IMetadataService, MetadataService>(Descriptor.Id);
        services.AddKeyedTransient<ITrackingService, TrackingService>(Descriptor.Id);
        services.AddTransient<ILocalTrackingService, TrackingService>();
        services.AddTransient<ILocalMetadataService, MetadataService>();
    }
}