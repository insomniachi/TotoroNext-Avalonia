using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimePahe;

public class Module : IModule
{
    public static Descriptor Descriptor { get; } = new()
    {
        Id = new Guid("25332fbc-aed7-4599-a076-40c6fae7ec2b"),
        Name = "Anime Pahe",
        Description =
            "AnimePahe is an encode \"group\" (me and my lovely bot), was founded in July 2014 by I, me and myself.\r\nWe encode on going anime, completed anime and anime movie.",
        HeroImage = ResourceHelper.GetResource("pahe.jpg"),
        Components = [ComponentTypes.AnimeProvider]
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddKeyedTransient<IAnimeProvider, AnimeProvider>(Descriptor.Id);

        services.AddHttpClient(Descriptor.Id.ToString(), client =>
        {
            client.BaseAddress = new Uri("https://animepahe.si/");
            client.DefaultRequestHeaders.Referrer = new Uri("https://animepahe.si/");
            client.DefaultRequestHeaders.Add(HeaderNames.Cookie, "__ddg2_=YW5pbWRsX3NheXNfaGkNCg.;");
        });

        services.AddHttpClient(KwikExtractor.ClientName, client =>
                {
                    client.DefaultRequestHeaders.Referrer = new Uri("https://kwik.cx/");
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(Http.UserAgent);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
    }
}