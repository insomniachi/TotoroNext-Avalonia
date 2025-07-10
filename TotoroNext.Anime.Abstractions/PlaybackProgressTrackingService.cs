using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public class PlaybackProgressTrackingService(IMessenger messenger) : IPlaybackProgressService,
                                                                     IRecipient<PlaybackState>,
                                                                     IRecipient<TrackingUpdated>
{
    private readonly string _file = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TotoroNext", $"progress.json");
    private Dictionary<string, ProgressInfo> _progress = [];

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
                Total = message.Duration.TotalSeconds,
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


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_file))
        {
            var text = await File.ReadAllTextAsync(_file, cancellationToken);
            _progress = JsonSerializer.Deserialize<Dictionary<string, ProgressInfo>>(text) ?? [];
        }

        messenger.Register<PlaybackState>(this);
        messenger.Register<TrackingUpdated>(this);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var completed = _progress.Where(x => x.Value.IsCompleted).Select(x => x.Key);
        foreach (var item in completed)
        {
            _progress.Remove(item);
        }

        await File.WriteAllTextAsync(_file, JsonSerializer.Serialize(_progress), cancellationToken);

        messenger.Unregister<PlaybackState>(this);
        messenger.Unregister<TrackingUpdated>(this);
    }
}

public class ProgressInfo
{
    public double Position { get; set; }

    public double Total { get; set; }

    [JsonIgnore]
    public bool IsCompleted { get; set; }
}
