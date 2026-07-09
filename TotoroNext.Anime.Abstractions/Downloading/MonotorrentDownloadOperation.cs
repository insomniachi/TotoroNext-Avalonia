using MonoTorrent.Client;
using TotoroNext.Anime.Abstractions.Models;
using Timer = System.Timers.Timer;

namespace TotoroNext.Anime.Abstractions.Downloading;

public class MonotorrentDownloadOperation(TorrentManager manager, string torrentFilePath, Episode episode, string downloadDir) : BaseDownloadOperation
{
    private Timer? _progressTimer;

    public override async Task StartAsync()
    {
        manager.TorrentStateChanged += TorrentStateChanged;
        TotalBytes = manager.Torrent?.Files.Sum(x => x.Length) ?? 0;
        StartProgressTracking();
        await manager.StartAsync();
    }

    private void StartProgressTracking()
    {
        _progressTimer = new Timer(500);
        _progressTimer.Elapsed += (_, _) =>
        {
            if (manager.State is not (TorrentState.Downloading or TorrentState.Seeding))
            {
                return;
            }

            Progress = manager.Progress;
            Speed = manager.Monitor.DownloadRate;
            DownloadedBytes = manager.Bitfield.TrueCount * manager.Torrent!.PieceLength;
        };
        _progressTimer.AutoReset = true;
        _progressTimer.Start();
    }

    private void StopProgressTracking()
    {
        if (_progressTimer == null)
        {
            return;
        }

        _progressTimer.Stop();
        _progressTimer.Dispose();
        _progressTimer = null;
    }

    private void TorrentStateChanged(object? sender, TorrentStateChangedEventArgs e)
    {
        switch (e.NewState)
        {
            case TorrentState.Downloading:
                DownloadStarted = true;
                OnStarted();
                break;
            case TorrentState.Seeding:
                Progress = 100;
                IsCompleted = true;
                StopProgressTracking();
                RenameDownloadedFile();
                Cleanup();
                OnCompleted();
                _ = manager.Engine?.RemoveAsync(manager);
                break;
            case TorrentState.Paused:
                IsPaused = true;
                break;
            case TorrentState.Stopped:
                IsCancelled = true;
                StopProgressTracking();
                Cleanup();
                break;
            case TorrentState.Error:
                StopProgressTracking();
                Cleanup();
                break;
        }
    }

    protected override async void TogglePauseResumeImpl()
    {
        try
        {
            switch (manager.State)
            {
                case TorrentState.Paused:
                    await manager.StartAsync();
                    IsPaused = false;
                    break;
                case TorrentState.Downloading:
                    await manager.PauseAsync();
                    IsPaused = true;
                    break;
            }
        }
        catch (Exception)
        {
            // Log error if needed
        }
    }

    protected override async void CancelImpl()
    {
        try
        {
            await manager.StopAsync();
            IsCancelled = true;
        }
        catch (Exception)
        {
            // Log error if needed
        }
    }

    private void Cleanup()
    {
        StopProgressTracking();

        try
        {
            if (File.Exists(torrentFilePath))
            {
                File.Delete(torrentFilePath);
            }
        }
        catch (Exception)
        {
            // Ignore cleanup errors
        }

        manager.TorrentStateChanged -= TorrentStateChanged;
    }

    private void RenameDownloadedFile()
    {
        try
        {
            if (manager.Torrent?.Files.Count != 1)
            {
                return;
            }

            var torrentFile = manager.Torrent.Files[0];
            var downloadedFile = Path.Combine(downloadDir, torrentFile.Path);

            if (!File.Exists(downloadedFile))
            {
                return;
            }

            var extension = Path.GetExtension(downloadedFile);
            var newFileName = $"{episode.Number}{extension}";
            var newFilePath = Path.Combine(downloadDir, newFileName);

            if (File.Exists(newFilePath))
            {
                File.Delete(newFilePath);
            }

            File.Move(downloadedFile, newFilePath, true);
        }
        catch (Exception)
        {
            // Log error if needed
        }
    }
}