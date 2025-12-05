using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Flurl.Http;
using TotoroNext.MediaEngine.Abstractions;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.MediaEngine.Vlc;

internal class VlcMediaPlayer(IModuleSettings<Settings> settings) : IMediaPlayer, ISeekable
{
    private readonly Subject<TimeSpan> _durationSubject = new();
    private readonly Subject<Unit> _playbackStoppedSubject = new();
    private readonly Subject<TimeSpan> _positionSubject = new();
    private readonly Settings _settings = settings.Value;
    private CompositeDisposable? _disposable;
    private Process? _process;
    private string _subtitleFile = "";
    private HttpInterface? _webInterface;

    public IObservable<TimeSpan> DurationChanged => _durationSubject;
    public IObservable<TimeSpan> PositionChanged => _positionSubject;
    public IObservable<Unit> PlaybackStopped => _playbackStoppedSubject;
    public IObservable<MediaPlayerState> StateChanged => Observable.Empty<MediaPlayerState>();

    public void Play(Media media, TimeSpan startPosition)
    {
        _disposable ??= [];
        _process?.Kill();

        var password = Guid.NewGuid().ToString();

        var startInfo = new ProcessStartInfo
        {
            FileName = _settings.FileName,
            ArgumentList =
            {
                media.Uri.ToString(),
                "--http-host=localhost",
                "--http-port=8080",
                "--extraintf=http",
                $"--meta-title={media.Metadata.Title}",
                $"--http-password={password}"
            }
        };

        if (_settings.LaunchFullScreen)
        {
            startInfo.ArgumentList.Add("--fullscreen");
        }

        if (media.Metadata.Headers?.TryGetValue("user-agent", out var userAgent) == true)
        {
            startInfo.ArgumentList.Add($"--http-user-agent={userAgent}");
        }

        if (media.Metadata.Headers?.TryGetValue("referer", out var referer) == true)
        {
            startInfo.ArgumentList.Add($"--http-referrer={referer}");
        }

        if (!string.IsNullOrEmpty(media.Metadata.Subtitle))
        {
            var content = media.Metadata.Subtitle.GetStringAsync().GetAwaiter().GetResult();
            _subtitleFile = Path.GetTempFileName();
            File.WriteAllText(_subtitleFile, content);
            startInfo.ArgumentList.Add($"--sub-file={_subtitleFile}");
        }

        if (startPosition > TimeSpan.Zero)
        {
            startInfo.ArgumentList.Add($"--start-time={startPosition.TotalSeconds}");
        }

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.Exited += (_, _) =>
        {
            _playbackStoppedSubject.OnNext(Unit.Default);
            if (File.Exists(_subtitleFile))
            {
                File.Delete(_subtitleFile);
            }

            _disposable.Dispose();
            _disposable = [];
        };
        _process.Start();

        _webInterface = new HttpInterface(_process, password);
        _webInterface.DurationChanged.Subscribe(_durationSubject.OnNext).DisposeWith(_disposable);
        _webInterface.PositionChanged.Subscribe(_positionSubject.OnNext).DisposeWith(_disposable);
    }

    public async Task SeekTo(TimeSpan position)
    {
        if (_webInterface is null)
        {
            return;
        }

        await _webInterface.SeekTo(position);
    }
}