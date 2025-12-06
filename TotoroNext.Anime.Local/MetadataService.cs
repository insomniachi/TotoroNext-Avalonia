using FuzzySharp;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using LiteDB;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Anime.Abstractions.Extensions;
using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module;

namespace TotoroNext.Anime.Local;

public class MetadataService : IMetadataService
{
    private static readonly Lazy<GraphQLHttpClient> ClientLazy = new(new GraphQLHttpClient("https://graphql.anilist.co/", new NewtonsoftJsonSerializer(), new HttpClient()));
    
    public Guid Id => Guid.Empty;

    public string Name => "Local";

    public async Task<AnimeModel> GetAnimeAsync(long id)
    {
        var tcs = new TaskCompletionSource<AnimeModel>();

        await Task.Run(() =>
        {
            using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
            var collection = db.GetCollection<LocalAnimeModel>().IncludeExtras();
            var anime = collection.FindById(id);
            tcs.SetResult(LocalModelConverter.ToAnimeModel(anime, collection));
        });

        return await tcs.Task;
    }

    public async Task<List<AnimeModel>> SearchAnimeAsync(string term)
    {
        var tcs = new TaskCompletionSource<List<AnimeModel>>();

        await Task.Run(() =>
        {
            using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
            var collection = db.GetCollection<LocalAnimeModel>().IncludeExtras();
            var prefix = term.Length >= 3 ? term[..3] : term;
            var candidates = collection.Find(Query.Contains("Title", prefix)).Take(50);
            var results = candidates
                          .Select(a => new { Anime = a, Score = Fuzz.PartialRatio(a.Title, term) })
                          .Where(x => x.Score >= 70)
                          .OrderByDescending(x => x.Score)
                          .Select(x => x.Anime)
                          .Select(x => LocalModelConverter.ToAnimeModel(x, collection)).ToList();
            tcs.SetResult(results);
        });

        return await tcs.Task;
    }

    public async Task<List<AnimeModel>> SearchAnimeAsync(AdvancedSearchRequest request)
    {
        if (request.IsEmpty())
        {
            return [];
        }

        var tcs = new TaskCompletionSource<List<AnimeModel>>();

        await Task.Run(() =>
        {
            using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
            var collection = db.GetCollection<LocalAnimeModel>().IncludeExtras();
            var term = request.Title;
            var candidates = string.IsNullOrEmpty(term)
                ? collection.FindAll()
                : collection.Find(Query.Contains("Title", term.Length >= 3 ? term[..3] : term))
                            .Select(a => new { Anime = a, Score = Fuzz.PartialRatio(a.Title, term) })
                            .Where(x => x.Score >= 70)
                            .OrderByDescending(x => x.Score)
                            .Select(x => x.Anime);

            if (request.MinYear.HasValue)
            {
                candidates = candidates.Where(x => x.Season?.Year >= request.MinYear.Value);
            }

            if (request.MaxYear.HasValue)
            {
                candidates = candidates.Where(x => x.Season?.Year <= request.MaxYear.Value);
            }

            if (request.SeasonName is { } season)
            {
                candidates = candidates.Where(x => x.Season?.SeasonName == season);
            }

            if (request.MinimumScore.HasValue)
            {
                candidates = candidates.Where(x => x.MeanScore >= request.MinimumScore.Value);
            }

            if (request.MaximumScore.HasValue)
            {
                candidates = candidates.Where(x => x.MeanScore <= request.MaximumScore.Value);
            }

            if (request.IncludedGenres is { Count: > 0 })
            {
                candidates = candidates.Where(x => request.IncludedGenres.All(tag => x.Genres.Contains(tag)));
            }

            if (request.ExcludedGenres is { Count: > 0 })
            {
                candidates = candidates.Where(x => request.ExcludedGenres.All(tag => !x.Genres.Contains(tag)));
            }

            var response = candidates.OrderByDescending(x => x.MeanScore)
                                     .Where(x => x is { MyAnimeListId: > 0, AnilistId: > 0 })
                                     .Take(100)
                                     .Select(x => LocalModelConverter.ToAnimeModel(x, collection))
                                     .ToList();
            
            tcs.SetResult(response);
        });

        return await tcs.Task;
    }


    public async Task<List<EpisodeInfo>> GetEpisodesAsync(AnimeModel anime)
    {
        if (anime.Episodes is { Count: > 0 })
        {
            return anime.Episodes;
        }

        var infos = await anime.GetEpisodes();
        using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
        var collection = db.GetCollection<LocalEpisodeInfo>();
        collection.Upsert(new LocalEpisodeInfo
        {
            Id = anime.Id,
            Info = infos,
            ExpiresAt = DateTimeOffset.Now.AddDays(3)
        });

        return infos;
    }

    public async Task<List<CharacterModel>> GetCharactersAsync(long animeId)
    {
        using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
        var collection = db.GetCollection<LocalAnimeModel>().IncludeExtras();
        var anime = collection.FindById(animeId);
       
        if (anime.CharacterInfo is { Characters.Count: > 0 })
        {
            return anime.CharacterInfo.Characters;
        }
        
        var characters = await AnilistHelper.GetCharactersAsync(ClientLazy.Value, animeId);
        var charCollection = db.GetCollection<LocalCharacterInfo>();
        
        charCollection.Upsert(new LocalCharacterInfo
        {
            Id = animeId,
            Characters = characters,
            ExpiresAt = DateTimeOffset.Now.AddDays(3)
        });
        
        return characters;
    }

    public async Task<List<string>> GetGenresAsync()
    {
        var tcs = new TaskCompletionSource<List<string>>();

        await Task.Run(() =>
        {
            using var db = new LiteDatabase(FileHelper.GetPath("animeData.db"));
            var collection = db.GetCollection<LocalAnimeModel>();
            var genres = collection.Find(x => x.Genres.Count > 0)
                                   .SelectMany(x => x.Genres)
                                   .ToHashSet();
            tcs.SetResult([..genres]);
        });

        return await tcs.Task;
    }
}