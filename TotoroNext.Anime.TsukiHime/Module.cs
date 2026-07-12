using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.TsukiHime.ViewModels;
using TotoroNext.Anime.TsukiHime.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.TsukiHime;

public class Module : IModule<Settings>
{
    public static readonly Guid Id = new("5078da1b-71b9-454f-904e-f6ced46bd28e");

    public Descriptor Descriptor { get; } = new()
    {
        Name = "TsukiHime",
        Id = Id,
        Components = [ComponentTypes.AnimeProvider],
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        TsukiHimeLocalData.Descriptor = Descriptor;
        
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        services.AddHttpClient($"{Id}-api", client =>
        {
            client.BaseAddress = new Uri(AnimeProvider.BaseUrl);
        });
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddTransient<IInitializer, Initializer>();
    }
}

public class Settings : OverridableConfig
{
    public string Group { get; set; } = "Erai-raws";

    [AllowedValues("1080", "720", "480")]
    public string Resolution { get; set; } = "1080";

    protected override void ConfigureProperty(ModuleOptionBuilder builder, PropertyInfo info)
    {
        if (info.Name == nameof(Group))
        {
            builder.WithAllowedValues(TsukiHimeLocalData.Groups.Select(x => x.Name));
        }
    }
}