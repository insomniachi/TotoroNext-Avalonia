using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Abstractions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInternalMediaPlayer(this IServiceCollection services)
    {
        services.AddTransient<IInternalMediaPlayer, InternalMediaPlayer>();
        services.AddKeyedTransient<IMediaPlayer, InternalMediaPlayer>(Guid.Empty);
        services.AddTransient<IBackgroundInitializer, BackgroundInitializer>();
        
        return services;
    }
}