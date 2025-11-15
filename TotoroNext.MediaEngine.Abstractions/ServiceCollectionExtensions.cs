using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Abstractions;

public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		public IServiceCollection AddInternalMediaPlayer()
		{
			services.AddTransient<IEmbeddedVlcMediaPlayer, EmbeddedVlcMediaPlayer>();
			services.AddKeyedTransient<IMediaPlayer, EmbeddedVlcMediaPlayer>(Guid.Empty);
			services.AddTransient<IBackgroundInitializer, BackgroundInitializer>();

			return services;
		}
	}
}