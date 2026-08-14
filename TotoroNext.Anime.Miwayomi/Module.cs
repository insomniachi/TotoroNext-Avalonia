
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Miwayomi.ViewModels;
using TotoroNext.Anime.Miwayomi.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Miwayomi;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Name = "Miwayomi",
        Id = new Guid("905e1602-b9a1-4ef9-8ce5-228e56acb8ff"),
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("miwayomi.png")
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
    }
    

}

public class Settings : OverridableConfig
{
    public string BaseUrl { get; set; } = "";
    public string Repository { get; set; } = "";
    public string SelectedSource { get; set; } = "";
}