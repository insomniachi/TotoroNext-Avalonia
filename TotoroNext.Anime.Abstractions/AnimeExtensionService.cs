using System.Text.Json;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class AnimeExtensionService : IAnimeExtensionService
{
    private readonly string _file =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext",
                     "extensions.json");

    private readonly Dictionary<long, AnimeOverrides> _extensions = [];
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

    public void Revert(long id)
    {
        if (!_extensions.TryGetValue(id, out var @override))
        {
            return;
        }

        @override.Revert();
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
    
    public string GetSelectedResult(AnimeModel anime)
    {
        return _extensions.GetValueOrDefault(anime.Id)?.SelectedResult ?? anime.Title;
    }

    public IAnimeProvider GetProvider(long id)
    {
        return _extensions.GetValueOrDefault(id)?.Provider is { } providerId
            ? _providerFactory.Create(providerId)
            : _providerFactory.CreateDefault()!;
    }
    
    public async Task<SearchResult?> SearchAndSelectAsync(AnimeModel anime)
    {
        var provider = GetProvider(anime.Id);
        var term = GetSelectedResult(anime);
        
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

        return await _selectAnimeDialog.GetValue(results);
    }

    public void CreateOrUpdateExtension(long id, AnimeOverrides overrides)
    {
        _extensions[id] = overrides;
        File.WriteAllText(_file, JsonSerializer.Serialize(_extensions));
    }
}