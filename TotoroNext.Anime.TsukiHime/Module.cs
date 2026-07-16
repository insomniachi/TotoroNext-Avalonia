using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.TsukiHime.ViewModels;
using TotoroNext.Anime.TsukiHime.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime.TsukiHime;

public class Module : IModule<Settings>
{
    public static readonly Guid Id = new("5078da1b-71b9-454f-904e-f6ced46bd28e");

    public Descriptor Descriptor { get; } = new()
    {
        Name = "TsukiHime",
        Id = Id,
        Components = [ComponentTypes.AnimeProvider, ComponentTypes.TorrentIndexer],
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("tsukihime.png")
    };

    public void ConfigureServices(IServiceCollection services)
    {
        TsukiHimeLocalData.Descriptor = Descriptor;

        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);
        services.AddHttpClient($"{Id}", client => { client.BaseAddress = new Uri(AnimeProvider.BaseUrl); });
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddTransient<IInitializer, Initializer>();
        services.AddKeyedTransient<ITorrentIndexer, Indexer>(Descriptor.Id);
    }
}

public class Settings : OverridableConfig
{
    [Description("Only torrents from this group would be returned by the provider")]
    public string Group { get; set; } = "Erai-raws";

    [AllowedValues("1080", "720", "480")] 
    [Description("First torrent of matching this resolution would be selected as default, remaining will be returned and used manually")]
    public string Resolution { get; set; } = "1080";

    protected override void ConfigureProperty(ModuleOptionBuilder builder, PropertyInfo info)
    {
        if (info.Name == nameof(Group))
        {
            builder.WithAllowedValues(TsukiHimeLocalData.Groups.Select(x => x.Name));
        }
    }
}