using System.Reactive.Concurrency;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions.Extensions;

public static class AnimeProviderExtensions
{
    extension(Episode ep)
    {
        public async Task<VideoServer?> SelectServer(CancellationToken ct)
        {
            var servers = await ep.GetServersAsync(ct).ToListAsync(ct);

            if (servers is not { Count: > 0 })
            {
                return null;
            }

            if (servers.Count == 1)
            {
                return servers[0];
            }

            return await Container.Services.GetRequiredService<ISelectionUserInteraction<VideoServer>>().GetValue(servers);
        }

        public async Task<List<VideoServer>> GetServers(CancellationToken ct)
        {
            try
            {
                return await ep.GetServersAsync(ct).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() => Container.Services.GetRequiredService<IDialogService>().Warning(ex.Message));
                return [];
            }
        }
    }

    extension(SearchResult result)
    {
        public async Task<List<Episode>> GetEpisodes(CancellationToken ct)
        {
            try
            {
                return await result.GetEpisodesAsync(ct).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() => Container.Services.GetRequiredService<IDialogService>().Warning(ex.Message));
                return [];
            }
        }
    }

    extension(IAnimeProvider? provider)
    {
        public async Task<List<SearchResult>> GetSearchResults(string? term, CancellationToken ct)
        {
            if (provider is null || string.IsNullOrEmpty(term))
            {
                return [];
            }

            try
            {
                return await provider.SearchAsync(term, ct).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() => Container.Services.GetRequiredService<IDialogService>().Warning(ex.Message));
                return [];
            }
        }
    }

    extension(VideoServer server)
    {
        public async Task<List<VideoSource>> GetSources()
        {
            try
            {
                return await server.Extract().ToListAsync();
            }
            catch (Exception ex)
            {
                RxApp.MainThreadScheduler.Schedule(() => Container.Services.GetRequiredService<IDialogService>().Warning(ex.Message));
                return [];
            }
        }
    }
}