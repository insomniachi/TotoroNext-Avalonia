using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Torrents.Abstractions;

namespace TotoroNext.Anime.Abstractions.Extensions;

public static class ServiceCollectionExtensions
{
	extension(IServiceCollection services)
	{
		public IServiceCollection AddDebrid()
		{
			return services.AddTransient<ITorrentExtractor, TorrentExtractor>()
						   .AddKeyedTransient<IDebrid, NoOpDebridService>(Guid.Empty);
		}
	}
}