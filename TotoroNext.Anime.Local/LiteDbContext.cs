using LiteDB;
using Microsoft.Extensions.Hosting;
using TotoroNext.Module;

namespace TotoroNext.Anime.Local;

internal class LiteDbContext : ILiteDbContext
{
    private readonly LiteDatabase _db = new (FileHelper.GetPath("animeData.db"));
    
    public ILiteCollection<LocalAnimeModel> Anime => _db.GetCollection<LocalAnimeModel>().IncludeExtras();

    public ILiteCollection<LocalTracking> Tracking => _db.GetCollection<LocalTracking>();
    
    public ILiteCollection<LocalEpisodeInfo> Episodes => _db.GetCollection<LocalEpisodeInfo>();
    
    public ILiteCollection<LocalCharacterInfo> Characters => _db.GetCollection<LocalCharacterInfo>();
    
    public ILiteCollection<LocalAdditionalInfo> AdditionalInfo => _db.GetCollection<LocalAdditionalInfo>();

    public bool HasData() => _db.GetCollectionNames().Any();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        Episodes.DeleteMany(x => x.ExpiresAt < now);
        Characters.DeleteMany(x => x.ExpiresAt < now);
        AdditionalInfo.DeleteMany(x => x.ExpiresAt < now);
        return Task.CompletedTask;
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _db.Dispose();
        return Task.CompletedTask;
    }
}

internal interface ILiteDbContext : IHostedService
{
    bool HasData();
    ILiteCollection<LocalAnimeModel> Anime { get; }
    ILiteCollection<LocalTracking> Tracking { get; }
    ILiteCollection<LocalEpisodeInfo> Episodes { get; }
    ILiteCollection<LocalCharacterInfo> Characters { get; }
    ILiteCollection<LocalAdditionalInfo> AdditionalInfo { get; }
}