using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;

namespace TotoroNext.MediaEngine.Vlc;

internal class HttpInterface
{
    private readonly string _password;
    private readonly string _api;
    private readonly Subject<TimeSpan> _durationChanged = new();
    private readonly Subject<TimeSpan> _timeChanged = new();
    private int _prevLength;
    private int _prevPosition;

    public IObservable<TimeSpan> DurationChanged => _durationChanged;
    public IObservable<TimeSpan> PositionChanged => _timeChanged;

    public HttpInterface(Process process, string password)
    {
        var host = "127.0.0.1";
        var port = "8080";
        _api = $"http://{host}:{port}";
        _password = password;

        Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(1))
            .Where(_ => !process.HasExited)
            .SelectMany(_ => GetStatus())
            .Where(s => s is not null)
            .Subscribe(status =>
            {
                if(status!.Length != _prevLength)
                {
                    _durationChanged.OnNext(TimeSpan.FromSeconds(status.Length));
                    _prevLength = status.Length;
                }

                if(status.Time !=  _prevPosition)
                {
                    _timeChanged.OnNext(TimeSpan.FromSeconds(status.Time));
                    _prevPosition = status.Time;
                }
            });
    }


    public async Task<VlcStatus?> GetStatus()
    {
        IFlurlResponse? result = null;

        try
        {
            result = await _api
                .AppendPathSegment("/requests/status.json")
                .WithBasicAuth("", _password).GetAsync();
        }
        catch { }

        if (result is null)
        {
            return null;
        }

        if (result.StatusCode >= 300)
        {
            return null;
        }

        return await result.GetJsonAsync<VlcStatus>();
    }

    public async Task SeekTo(TimeSpan timeSpan)
    {
        _ = await _api
             .AppendPathSegment("/requests/status.json")
             .SetQueryParam("command", "seek")
             .SetQueryParam("val", (int)timeSpan.TotalSeconds)
             .WithBasicAuth("", _password)
             .GetAsync();
    }

    public async Task SetVolume(int percent)
    {
        _ = await _api
         .AppendPathSegment("/requests/status.json")
         .SetQueryParam("command", "volume")
         .SetQueryParam("val", $"{percent}%")
         .WithBasicAuth("", _password)
         .GetAsync();
    }
}


internal class VlcStatus
{
    [JsonPropertyName("time")]
    public int Time { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    [JsonPropertyName("information")]
    public Information Information { get; set; } = new Information();
}

internal class Information
{
    [JsonPropertyName("category")]
    public Category Category { get; set; } = new();
}

internal class Category
{
    [JsonPropertyName("meta")]
    public Meta Meta { get; set; } = new();
}

internal class Meta
{
    [JsonPropertyName("filename")]
    public string FileName { get; set; } = string.Empty;
}
