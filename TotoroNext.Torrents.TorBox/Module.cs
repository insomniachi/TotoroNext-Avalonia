using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Torrents.Abstractions;
using TotoroNext.Torrents.TorBox.ViewModel;
using TotoroNext.Torrents.TorBox.Views;

namespace TotoroNext.Torrents.TorBox;

public class Module : IModule<Settings>
{
    public Descriptor Descriptor { get; } = new()
    {
        Name = "TorBox",
        Description = "TorBox debrid service integration",
        Components = [ComponentTypes.Debrid],
        Id = new Guid("2789535c-e32f-4e3c-ad2e-d45c84ec0f37"),
        SettingViewModel = typeof(SettingsViewModel),
        HeroImage = ResourceHelper.GetResource("torbox.jpg")
    };
    
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddHttpClient("TorBox", (sp, client) =>
        {
            var token = sp.GetRequiredService<IModuleSettings<Settings>>().Value.Token;
            client.BaseAddress = new Uri("https://api.torbox.app/v1/api/");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
        services.AddKeyedTransient<IDebrid, TorBoxService>(Descriptor.Id);
    }

}

public class Settings
{
    public string Token { get; set; } = "";
}