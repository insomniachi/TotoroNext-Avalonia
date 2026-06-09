using Microsoft.Extensions.DependencyInjection;
using MonoTorrent.Client;

namespace TotoroNext.Torrents.Abstractions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWebTorrent(this IServiceCollection services)
    {
        services.AddSingleton<ClientEngine>(_ => new ClientEngine(new EngineSettingsBuilder
        {
            AllowPortForwarding = true,
            AutoSaveLoadDhtCache = false,
            AutoSaveLoadFastResume = false,
            AutoSaveLoadMagnetLinkMetadata = false,
            HttpStreamingPrefix = "http://127.0.0.1:6969/",
        }.ToSettings()));
        
        return services;
    }
}