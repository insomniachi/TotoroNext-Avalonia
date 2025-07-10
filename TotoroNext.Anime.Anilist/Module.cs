using System.Net.Http.Headers;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anilist;

public class Module : IModule<Settings>
{
    internal static Guid Id { get; } = new Guid("b5d31e9b-b988-44e8-8e28-348f58cf1d04");

    public Descriptor Descriptor { get; } = new Descriptor
    {
        Id = Id,
        Name = @"Anilist",
        Components = [ComponentTypes.Metadata, ComponentTypes.Tracking],
        Description = "AniList: The next-generation anime platform Track, share, and discover your favorite anime and manga with AniList. Discover your obsessions. ",
        HeroImage = ResourceHelper.GetResource("anilist.jpg")
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);

        services.AddKeyedTransient<IMetadataService, AnilistMetadataService>(Descriptor.Id);
        services.AddKeyedTransient<ITrackingService, AnilistTrackingService>(Descriptor.Id);

        services.AddHttpClient(nameof(AnilistMetadataService), (sp, client) =>
        {
            var settings = sp.GetRequiredService<IModuleSettings<Settings>>();
            if (settings.Value.Auth is { } auth)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            }
        });
        services.AddTransient(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AnilistMetadataService));
            return new GraphQLHttpClient("https://graphql.anilist.co/", new NewtonsoftJsonSerializer(), httpClient);
        });
    }
}


public class Settings
{
    public AniListAuthToken? Auth { get; set; }
    public bool IncludeNsfw { get; set; }
    public double SearchLimit { get; set; } = 15;
    public TitleLanguage TitleLangauge { get; set; } = TitleLanguage.Romaji;
}

public class AniListAuthToken
{
    public string AccessToken { get; set; } = "";
    public long ExpiresIn { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum TitleLanguage
{
    English,
    Romaji
}
