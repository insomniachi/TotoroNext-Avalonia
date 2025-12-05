using FuzzySharp;
using LiteDB;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Local;

public class MetadataService : IMetadataService
{
    public Guid Id => Guid.Empty;
    
    public string Name => "Local";
    
    public Task<AnimeModel> GetAnimeAsync(long id)
    {
        using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
        var collection = db.GetCollection<LocalAnimeModel>();
        var anime = collection.Include(x => x.Tracking)
                              .FindById(id);
        return Task.FromResult(LocalModelConverter.ToAnimeModel(anime, collection));
    }
    
    public Task<List<AnimeModel>> SearchAnimeAsync(string term)
    {
        using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
        var collection = db.GetCollection<LocalAnimeModel>();
        var prefix = term.Length >= 3 ? term[..3] : term;
        var candidates = collection.Find(Query.Contains("Title", prefix));
        var results = candidates
                      .Select(a => new { Anime = a, Score = Fuzz.PartialRatio(a.Title, term) })
                      .Where(x => x.Score >= 70)
                      .OrderByDescending(x => x.Score)
                      .Select(x => x.Anime)
                      .Select(x => LocalModelConverter.ToAnimeModel(x, collection)).ToList();
        return Task.FromResult(results);
    }
    
    public Task<List<AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request)
    {
        return string.IsNullOrEmpty(request.Title) ? Task.FromResult<List<AnimeModel>>([]) : SearchAnimeAsync(request.Title);
    }
    
    public Task<List<EpisodeInfo>> GetEpisodesAsync(AnimeModel anime)
    {
        return anime.GetEpisodes();
    }
    
    public Task<List<CharacterModel>> GetCharactersAsync(long animeId)
    {
        return Task.FromResult<List<CharacterModel>>([]);
    }
    
    public Task<List<string>> GetGenresAsync()
    {
        return Task.FromResult<List<string>>([]);
    }
}