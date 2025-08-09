using System.Diagnostics;
using System.IO.Pipes;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using System.Text.Json;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Mpv;

internal class MpvMediaPlayer(IModuleSettings<Settings> settings) : IMediaPlayer, ISeekable
{
    private readonly Subject<TimeSpan> _durationSubject = new();
    private readonly Subject<Unit> _playbackStoped = new();
    private readonly Subject<TimeSpan> _positionSubject = new();
    private readonly Settings _settings = settings.Value;
    private NamedPipeClientStream? _ipcStream;
    private Process? _process;

    public IObservable<TimeSpan> DurationChanged => _durationSubject;
    public IObservable<TimeSpan> PositionChanged => _positionSubject;
    public IObservable<Unit> PlaybackStopped => _playbackStoped;

    public void Play(Media media, TimeSpan startPosition)
    {
        _process?.Kill();
        _ipcStream?.Dispose();

        var pipeName = $"mpv-pipe-{Guid.NewGuid()}";
        var pipePath = $@"\\.\pipe\{pipeName}";

        var startInfo = new ProcessStartInfo
        {
            FileName = _settings.FileName,
            ArgumentList =
            {
                media.Uri.ToString(),
                $"--title={media.Metadata.Title}",
                $"--force-media-title={media.Metadata.Title}",
                $"--input-ipc-server={pipePath}"
            }
        };

        if (startPosition.TotalSeconds > 0)
        {
            startInfo.ArgumentList.Add($"--start={startPosition.TotalSeconds}");
        }

        if (_settings.LaunchFullScreen)
        {
            startInfo.ArgumentList.Add("--fullscreen");
        }

        if (media.Metadata.Headers is { Count: > 0 } headers)
        {
            var headerFields = string.Join(" ", headers.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            startInfo.ArgumentList.Add($"--http-header-fields={headerFields}");
        }

        if (media.Metadata.MedaSections is { Count: > 0 } sections)
        {
            var file = ChapterFileWriter.CreateChapterFile(sections);
            startInfo.ArgumentList.Add($"--chapters-file={file}");
        }

        if (!string.IsNullOrEmpty(media.Metadata.Subtitle))
        {
            startInfo.ArgumentList.Add($"--sub-file={media.Metadata.Subtitle}");
        }

        _process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };
        _process.Exited += (_, _) => _playbackStoped.OnNext(Unit.Default);

        Task.Run(() => IpcLoop(_process, pipeName));
    }

    public async Task SeekTo(TimeSpan timestamp)
    {
        if (_ipcStream is null)
        {
            return;
        }

        await SendIpcCommand(_ipcStream, new { command = new object[] { "seek", timestamp.TotalSeconds, "absolute+exact" } });
    }

    private async Task IpcLoop(Process process, string pipeName)
    {
        NamedPipeClientStream? pipe = null;
        try
        {
            pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            _ipcStream = pipe;

            process.Exited += (_, _) =>
            {
                try
                {
                    pipe?.Dispose();
                }
                catch
                {
                    // ignored
                }
            };

            process.Start();
            // Retry until connected or process exits
            while (!await TryConnectPipeAsync(pipe))
            {
                if (process is { HasExited: true })
                {
                    return;
                }

                await Task.Delay(500);
            }

            using var reader = new StreamReader(pipe, Encoding.UTF8);

            while (pipe.IsConnected)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    await HandleIpcMessage(pipe, line);
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MpvMediaPlayer] IPC connection failed: {ex.Message}");
        }
        finally
        {
            if (pipe is not null)
            {
                await pipe.DisposeAsync();
            }
        }
    }

    private static async Task<bool> TryConnectPipeAsync(NamedPipeClientStream pipe)
    {
        try
        {
            await pipe.ConnectAsync(1000); // 1 second timeout per attempt
            return pipe.IsConnected;
        }
        catch (TimeoutException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static async Task SendIpcCommand(NamedPipeClientStream pipe, object command)
    {
        var json = JsonSerializer.Serialize(command) + "\n";
        var bytes = Encoding.UTF8.GetBytes(json);
        await pipe.WriteAsync(bytes);
        await pipe.FlushAsync();
    }

    private async Task HandleIpcMessage(NamedPipeClientStream pipe, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("event", out var evt))
        {
            var eventType = evt.GetString();

            if (eventType == "property-change")
            {
                var name = root.GetProperty("name").GetString();
                var data = root.GetProperty("data");
                if (name == "duration" && data.ValueKind == JsonValueKind.Number)
                {
                    _durationSubject.OnNext(TimeSpan.FromSeconds(data.GetDouble()));
                }
                else if (name == "time-pos" && data.ValueKind == JsonValueKind.Number)
                {
                    _positionSubject.OnNext(TimeSpan.FromSeconds(data.GetDouble()));
                }
            }
            else if (eventType == "file-loaded")
            {
                // Observe properties
                await SendIpcCommand(pipe, new { command = new object[] { "observe_property", 1, "duration" } });
                await SendIpcCommand(pipe, new { command = new object[] { "observe_property", 2, "time-pos" } });
            }
        }
    }
}