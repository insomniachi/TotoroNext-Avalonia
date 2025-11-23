using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TotoroNext.SongRecognition.Capture_Helpers;

internal class WasapiCaptureHelper : ICaptureHelper
{
    private readonly WasapiCapture _capture;
    private readonly BufferedWaveProvider _captureBuf;
    private readonly MediaFoundationResampler _resampler;

    public WasapiCaptureHelper()
    {
        _capture = WasApiLoopBackHelper.Loopback ? new WasapiLoopbackCapture() : new WasapiCapture();
        _captureBuf = new BufferedWaveProvider(_capture.WaveFormat) { ReadFully = false };
        _resampler = new MediaFoundationResampler(_captureBuf, ICaptureHelper.WaveFormat);
    }

    public ISampleProvider? SampleProvider { get; private set; }

    public void Dispose()
    {
        _resampler.Dispose();
        _capture.Dispose();
    }


    public bool Live => true;
    public Exception? Exception { get; private set; }

    public void Start()
    {
        _capture.DataAvailable += (_, e) => { _captureBuf.AddSamples(e.Buffer, 0, e.BytesRecorded); };
        _capture.RecordingStopped += (_, e) => { Exception = e.Exception; };
        _capture.StartRecording();

        SampleProvider = _resampler.ToSampleProvider();
    }
}