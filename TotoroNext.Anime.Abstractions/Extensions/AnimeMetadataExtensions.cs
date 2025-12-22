using System.Text.Json;
using System.Text.Json.Nodes;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions.Extensions;

public static class AnimeMetadataExtensions
{
    extension(IMetadataService provider)
    {
        public async Task<Models.AnimeModel?> SearchAndSelectAsync(SearchResult model)
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

            return await Container.Services.GetRequiredService<ISelectionUserInteraction<Models.AnimeModel>>().GetValue(results);
        }
    }

    extension(Models.AnimeModel anime)
    {
        public async Task<List<EpisodeInfo>> GetEpisodes(CancellationToken ct = default)
        {
            try
            {
                var serviceType = anime.ServiceName switch
                {
                    "Anilist" => "anilist_id",
                    "MyAnimeList" or "Local" => "mal_id",
                    _ => throw new NotSupportedException($"Service type {anime.ServiceId} is not supported.")
                };

                var today = TimeProvider.System.GetUtcNow();

                var response = await @"https://api.ani.zip/mappings".SetQueryParam(serviceType, anime.Id).GetStringAsync(cancellationToken: ct);
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

                    if (property.Key.StartsWith('S'))
                    {
                        ep.IsSpecial = true;
                    }

                    result.Add(ep);
                }

                var first = result.FirstOrDefault();
                if (first is null)
                {
                    return result;
                }

                var max = result.Max(x => x.EpisodeNumber);
                var firstEp = first.EpisodeNumber;
                foreach (var episode in result)
                {
                    if (episode.EpisodeNumber < firstEp)
                    {
                        episode.EpisodeNumber = max + 1;
                    }
                    else
                    {
                        episode.EpisodeNumber -= firstEp - 1;
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return [];
            }
        }
    }
}