using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.ViewModels;
using TotoroNext.Anime.Abstractions.Views;
using TotoroNext.Anime.Local.ViewModels;
using TotoroNext.Anime.Local.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using SettingsViewModel = TotoroNext.Anime.Local.ViewModels.SettingsViewModel;

namespace TotoroNext.Anime.Local;

public class Module : IModule<Settings>
{
    public static Guid Id { get; } = new Guid("5500de7e-4268-4edf-afcd-d445fec437e1");
    
    public Descriptor Descriptor { get; } = new()
    {
        Id = Id,
        Name = "Offline Anime Database",
        Components = [ComponentTypes.Metadata, ComponentTypes.Tracking],
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("oad.png")
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedViewMap<UpdateTrackingView, UpdateTrackingViewModel>("tracking/Local");
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddKeyedViewMap<EditAnimeView, EditAnimeViewModel>("EditAnime");

        services.AddSingleton<IDbContext, DbContext>();
        services.AddTransient<IAnimeMappingService, AnimeMappingService>();

        services.AddKeyedTransient<IMetadataService, MetadataService>(Id);
        services.AddKeyedTransient<ITrackingService, TrackingService>(Id);
        services.AddTransient<ILocalTrackingService, TrackingService>();
        services.AddTransient<ILocalMetadataService, MetadataService>();
    }
}

internal class Settings : OverridableConfig
{
    [DisplayName("Simkl Client Id")]
    [Description("Required to map Simkl Id's")]
    public string SimklClientId { get; set; } = "";
}