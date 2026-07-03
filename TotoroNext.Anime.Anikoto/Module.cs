using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Anikoto.ViewModels;
using TotoroNext.Anime.Anikoto.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anikoto;

public class Module : IModule<Settings>
{
    public static readonly Guid Id = new("56244ee0-4460-40b7-84cf-af3819d2429a");
    
    public Descriptor Descriptor { get; } = new()
    {
        Name = "Anikoto",
        Id = Id,
        Components = [ComponentTypes.AnimeProvider, ComponentTypes.AnimeDownloader],
        HeroImage = ResourceHelper.GetResource("anikoto.png"),
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        services.AddHttpClient($"{Id}-api",client =>
        {
            client.BaseAddress = new Uri(AnimeProvider.BaseUrl);
            client.DefaultRequestHeaders.Referrer = client.BaseAddress;
            client.DefaultRequestHeaders.UserAgent.ParseAdd(Http.UserAgent);
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        });
        services.AddViewMap<SettingsView, SettingsViewModel>();
    }
}

public class Settings : OverridableConfig
{
    [AllowedValues("sub", "hsub", "dub")]
    [DisplayName("Preferred Stream Category")]
    public string PreferredStreamCategory { get; set; } = "sub";

    [AllowedValues("VidPlay-1", "HD-1", "Vidstream-2", "VidCloud-1")]
    [DisplayName("Preferred Server")]
    public string PreferredServer { get; set; } = "HD-1";
}