using System.Net.Http.Headers;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using IconPacks.Avalonia.ForkAwesome;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.ViewModels;
using TotoroNext.Anime.Abstractions.Views;
using TotoroNext.Anime.Anilist.ViewModels;
using TotoroNext.Anime.Anilist.Views;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anilist;

public sealed class Module : IModule<Settings>
{
    internal static Guid Id { get; } = new("b5d31e9b-b988-44e8-8e28-348f58cf1d04");

    public Descriptor Descriptor { get; } = new()
    {
        Id = Id,
        Name = @"Anilist",
        Components = [ComponentTypes.Metadata, ComponentTypes.Tracking],
        Description =
            "AniList: The next-generation anime platform Track, share, and discover your favorite anime and manga with AniList. Discover your obsessions. ",
        HeroImage = ResourceHelper.GetResource("anilist.jpg"),
        SettingViewModel = typeof(SettingsViewModel)
    };

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient(_ => Descriptor);
        services.AddModuleSettings(this);
        services.AddViewMap<SettingsView, SettingsViewModel>();
        services.AddKeyedViewMap<UpdateTrackingView, UpdateTrackingViewModel>($"tracking/{nameof(ExternalIds.Anilist)}");
        services.AddMainNavigationItem<AiringScheduleView, AiringScheduleViewModel>("Schedule", PackIconForkAwesomeKind.Calendar);

        services.AddTransient<IAnilistMetadataService, AnilistMetadataService>();
        services.AddKeyedTransient<IMetadataService, AnilistMetadataService>(Descriptor.Id);
        services.AddKeyedTransient<ITrackingService, AnilistTrackingService>(Descriptor.Id);

        services.AddHttpClient(nameof(AnilistMetadataService), (sp, client) =>
        {
            var settings = sp.GetRequiredService<IModuleSettings<Settings>>();
            if (settings.Value.Token is { } token)
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        });
        services.AddTransient(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AnilistMetadataService));
            return new GraphQLHttpClient("https://graphql.anilist.co/", new NewtonsoftJsonSerializer(), httpClient);
        });
    }
}

public sealed class Settings
{
    public const int ClientId = 10588;
    public string? Token { get; set; }
    public bool IncludeNsfw { get; set; }
    public double SearchLimit { get; set; } = 15;
    public TitleLanguage TitleLanguage { get; set; } = TitleLanguage.Romaji;
}

public enum TitleLanguage
{
    English,
    Romaji
}