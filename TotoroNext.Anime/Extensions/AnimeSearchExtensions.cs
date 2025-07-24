using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Extensions;

internal static class AnimeSearchExtensions
{
    internal static Task<SearchResult?> SearchAndSelectAsync(this IAnimeProvider provider, AnimeModel model)
    {
        return SearchAndSelectAsync(provider, model.Title);
    }
    
    internal static async Task<SearchResult?> SearchAndSelectAsync(this IAnimeProvider provider, string term)
    {
        var results = await provider.SearchAsync(term).ToListAsync();

        switch (results.Count)
        {
            case 0:
                return null;
            case 1:
                return results[0];
        }

        if (results.FirstOrDefault(x => string.Equals(x.Title, term, StringComparison.OrdinalIgnoreCase)) is { } result)
        {
            return result;
        }

        return await Container.Services.GetRequiredService<ISelectionUserInteraction<SearchResult>>().GetValue(results);
    }

    internal static async Task<AnimeModel?> SearchAndSelectAsync(this IMetadataService provider, SearchResult model)
    {
        var results = await provider.SearchAnimeAsync(model.Title);

        switch (results.Count)
        {
            case 0:
                return null;
            case 1:
                return results[0];
        }

        if (results.FirstOrDefault(x => string.Equals(x.Title, model.Title, StringComparison.OrdinalIgnoreCase)) is { } result)
        {
            return result;
        }

        return await Container.Services.GetRequiredService<ISelectionUserInteraction<AnimeModel>>().GetValue(results);
    }

    internal static async Task<VideoServer?> SelectServer(this Episode ep)
    {
        var servers = await ep.GetServersAsync().ToListAsync();

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

    internal static async Task<List<EpisodeInfo>> GetEpisodes(this AnimeModel anime)
    {
        var serviceType = anime.ServiceName switch
        {
            "Anilist" => "anilist_id",
            "MyAnimeList" => "mal_id",
            _ => throw new NotSupportedException($"Service type {anime.ServiceId} is not supported.")
        };

        var today = TimeProvider.System.GetUtcNow();

        var response = await @"https://api.ani.zip/mappings".SetQueryParam(serviceType, anime.Id).GetStringAsync();
        var jObject = (JsonObject)JsonNode.Parse(response)!;
        var episodesObj = jObject["episodes"]!.AsObject();

        var result = new List<EpisodeInfo>();

        foreach (var property in episodesObj)
        {
            if (property.Value.Deserialize<EpisodeInfo>() is not { } ep)
            {
                continue;
            }

            if (ep.AirDateUtc is null || ep.AirDateUtc > today)
            {
                continue;
            }

            result.Add(ep);
        }

        return result;
    }
}
