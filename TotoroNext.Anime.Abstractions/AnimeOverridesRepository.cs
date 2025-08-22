using System.Text.Json;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public class AnimeOverridesRepository : IAnimeOverridesRepository
{
    private readonly string _file =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext",
                     "overrides.json");

    private readonly Dictionary<long, AnimeOverrides> _overrides = [];

    public AnimeOverridesRepository()
    {
        if (File.Exists(_file))
        {
            _overrides = JsonSerializer.Deserialize<Dictionary<long, AnimeOverrides>>(File.ReadAllText(_file)) ?? [];
        }
    }

    public void Revert(long id)
    {
        if (!_overrides.TryGetValue(id, out var @override))
        {
            return;
        }

        @override.Revert();
    }

    public bool Remove(long id)
    {
        return _overrides.Remove(id);
    }

    public AnimeOverrides? GetOverrides(long id)
    {
        return _overrides.GetValueOrDefault(id);
    }

    public void CreateOrUpdate(long id, AnimeOverrides overrides)
    {
        _overrides[id] = overrides;
        File.WriteAllText(_file, JsonSerializer.Serialize(_overrides));
    }
}