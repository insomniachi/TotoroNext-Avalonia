using NAudio.Wave;

namespace TotoroNext.SongRecognition;

internal interface ICaptureHelper : IDisposable
{
    static readonly WaveFormat WaveFormat = new(Analysis.SampleRate, 16, 1);

    bool Live { get; }
    ISampleProvider? SampleProvider { get; }
    Exception? Exception { get; }
    void Start();
}