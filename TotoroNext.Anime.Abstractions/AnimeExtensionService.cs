using System.Text.Json;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class AnimeExtensionService : IAnimeExtensionService
{
    private readonly Dictionary<long, AnimeOverrides> _extensions = [];

    private readonly string _file =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext",
                     "extensions.json");

    private readonly IFactory<IAnimeProvider, Guid> _providerFactory;
    private readonly ISelectionUserInteraction<SearchResult> _selectAnimeDialog;

    public AnimeExtensionService(IFactory<IAnimeProvider, Guid> providerFactory,
                                 ISelectionUserInteraction<SearchResult> selectAnimeDialog)
    {
        _providerFactory = providerFactory;
        _selectAnimeDialog = selectAnimeDialog;
        if (File.Exists(_file))
        {
            _extensions = JsonSerializer.Deserialize<Dictionary<long, AnimeOverrides>>(File.ReadAllText(_file)) ?? [];
        }
    }

    public void RemoveExtension(long id)
    {
        _extensions.Remove(id);
    }

    public AnimeOverrides? GetExtension(long id)
    {
        return _extensions.GetValueOrDefault(id);
    }

    public bool IsInIncognitoMode(long id)
    {
        return _extensions.GetValueOrDefault(id)?.IsNsfw ?? false;
    }

    public async Task<SearchResult?> SearchOrSelectAsync(Models.AnimeModel anime)
    {
        var provider = GetProvider(anime.Id);
        var searchResult = GetSearchResult(anime.Id);

        if (searchResult is not null)
        {
            return new SearchResult(provider, searchResult.Id, searchResult.Title);
        }
        
        var results = await provider.GetSearchResults(anime.Title, CancellationToken.None);

        if (TryFindMatch(results, anime, anime.Title) is { } result)
        {
            return result;
        }

        return await _selectAnimeDialog.GetValue(results);
    }

    public async Task<SearchResult?> SearchAsync(Models.AnimeModel anime)
    {
        var provider = GetProvider(anime.Id);
        var searchResult = GetSearchResult(anime.Id);
        
        if (searchResult is not null)
        {
            return new SearchResult(provider, searchResult.Id, searchResult.Title);
        }
        
        var results = await provider.GetSearchResults(anime.Title, CancellationToken.None);
        return TryFindMatch(results, anime, anime.Title);
    }

    public void CreateOrUpdateExtension(long id, AnimeOverrides overrides)
    {
        _extensions[id] = overrides;
        File.WriteAllText(_file, JsonSerializer.Serialize(_extensions));
    }

    public IAnimeProvider GetProvider(long id)
    {
        var extension = _extensions.GetValueOrDefault(id);

        if (extension is null or { Provider: null })
        {
            return _providerFactory.CreateDefault()!;
        }

        var provider = _providerFactory.Create(extension.Provider.Value);
        provider.UpdateOptions(extension.AnimeProviderOptions);
        return provider;
    }

    private ProviderItemResult? GetSearchResult(long id)
    {
        var extension = _extensions.GetValueOrDefault(id);
        return extension?.ProviderResult;
    }

    private static SearchResult? TryFindMatch(List<SearchResult> results, Models.AnimeModel anime, string term)
    {
        switch (results.Count)
        {
            case 0:
                return null;
            case 1:
                return results[0];
        }

        if (results.FirstOrDefault(x => x.ExternalId.GetIdForService(anime.ServiceName ?? "") == anime.Id) is { } exactMatch)
        {
            return exactMatch;
        }

        if (results.FirstOrDefault(x => string.Equals(x.Title, term, StringComparison.OrdinalIgnoreCase)) is { } result)
        {
            return result;
        }

        return null;
    }
}