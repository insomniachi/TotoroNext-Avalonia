using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public class PlaybackProgressTrackingService : IPlaybackProgressService,
                                               IRecipient<PlaybackState>,
                                               IRecipient<TrackingUpdated>
{
    private readonly string _file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext",
                                                 "progress.json");

    private readonly IMessenger _messenger;
    private readonly Dictionary<string, ProgressInfo> _progress = [];

    public PlaybackProgressTrackingService(IMessenger messenger)
    {
        _messenger = messenger;

        if (!File.Exists(_file))
        {
            return;
        }

        var text = File.ReadAllText(_file);
        _progress = JsonSerializer.Deserialize<Dictionary<string, ProgressInfo>>(text) ?? [];
    }

    public Dictionary<float, ProgressInfo> GetProgress(long id)
    {
        var keys = _progress.Keys.Where(x => x.StartsWith($"{id}_")).ToList();
        var result = new Dictionary<float, ProgressInfo>();
        foreach (var key in keys)
        {
            var parts = key.Split('_');
            if (parts.Length < 2 || !float.TryParse(parts[1], out var episodeNumber))
            {
                continue;
            }

            if (_progress.TryGetValue(key, out var info))
            {
                result[episodeNumber] = info;
            }
        }

        return result;
    }


    public Task StartAsync(CancellationToken cancellationToken)
    {
        _messenger.Register<PlaybackState>(this);
        _messenger.Register<TrackingUpdated>(this);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var completed = _progress.Where(x => x.Value.IsCompleted).Select(x => x.Key);
        foreach (var item in completed)
        {
            _progress.Remove(item);
        }

        await File.WriteAllTextAsync(_file, JsonSerializer.Serialize(_progress), cancellationToken);

        _messenger.Unregister<PlaybackState>(this);
        _messenger.Unregister<TrackingUpdated>(this);
    }

    public void Receive(PlaybackState message)
    {
        if (message.Position < TimeSpan.FromSeconds(30))
        {
            return;
        }

        var key = $"{message.Anime.Id}_{message.Episode.Number}";
        if (_progress.TryGetValue(key, out var info))
        {
            info.Position = message.Position.TotalSeconds;
        }
        else
        {
            _progress[key] = new ProgressInfo
            {
                Position = message.Position.TotalSeconds,
                Total = message.Duration.TotalSeconds
            };
        }
    }

    public void Receive(TrackingUpdated message)
    {
        var key = $"{message.Anime.Id}_{message.Episode.Number}";
        if (_progress.TryGetValue(key, out var info))
        {
            info.IsCompleted = true;
        }
    }
}

public class ProgressInfo
{
    public double Position { get; set; }

    public double Total { get; set; }

    [JsonIgnore] public bool IsCompleted { get; set; }
}