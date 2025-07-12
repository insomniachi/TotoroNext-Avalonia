using Microsoft.Extensions.DependencyInjection;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Aniskip;

public partial class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("5ccd59c9-7fd1-485e-b542-e4b8cfaf5655"),
        Name = "Aniskip",
        Components = [ComponentTypes.MediaSegments],
        Description = "third party api to allow users to submit and vote on timestamp ranges for segments of episodes to skip.",
        HeroImage = ResourceHelper.GetResource("aniskip.png")
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpClient(nameof(AniskipClient), client =>
        {
            client.BaseAddress = new Uri("https://api.aniskip.com/");
        });
        services.AddTransient<IAniskipClient, AniskipClient>();
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<IMediaSegmentsProvider, MediaSegmentsProvider>(Descriptor.Id);
    }
}
