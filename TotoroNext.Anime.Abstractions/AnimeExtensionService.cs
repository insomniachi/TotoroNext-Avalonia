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

    public string GetSearchTerm(AnimeModel anime)
    {
        var extension = _extensions.GetValueOrDefault(anime.Id);

        return string.IsNullOrEmpty(extension?.SelectedProviderResult)
            ? anime.Title
            : extension.SelectedProviderResult;
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

    public async Task<SearchResult?> SearchAndSelectAsync(AnimeModel anime)
    {
        var provider = GetProvider(anime.Id);
        var term = GetSearchTerm(anime);

        var results = await provider.GetSearchResults(term);

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

        return await _selectAnimeDialog.GetValue(results);
    }

    public async Task<SearchResult?> SearchAsync(AnimeModel anime)
    {
        var provider = GetProvider(anime.Id);
        var term = GetSearchTerm(anime);

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

        return null;
    }

    public void CreateOrUpdateExtension(long id, AnimeOverrides overrides)
    {
        _extensions[id] = overrides;
        File.WriteAllText(_file, JsonSerializer.Serialize(_extensions));
    }
}