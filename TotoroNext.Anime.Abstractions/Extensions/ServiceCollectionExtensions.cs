using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime.Abstractions.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDebrid(this IServiceCollection services)
    {
        return services.AddTransient<ITorrentExtractor, TorrentExtractor>()
                       .AddKeyedTransient<IDebrid, NoOpDebridService>(Guid.Empty);
    }
}