using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace TotoroNext.SongRecognition;

internal class Analysis
{
    public const int SampleRate = 16000;
    public const int ChunksPerSecond = 125;
    public const int ChunkSize = SampleRate / ChunksPerSecond;
    public const int WindowSize = ChunkSize * 16;
    public const int BinCount = WindowSize / 2 + 1;
    private static readonly float[] Hann = Array.ConvertAll(Window.Hann(WindowSize), Convert.ToSingle);
    private readonly Complex32[] _fftBuf = new Complex32[WindowSize];
    private readonly List<float[]> _stripes = new(3 * ChunksPerSecond);
    private readonly float[] _windowRing = new float[WindowSize];
    private Action _stripeAddedCallback;

    public int ProcessedSamples { get; private set; }
    public int ProcessedMs => ProcessedSamples * 1000 / SampleRate;
    public int StripeCount => _stripes.Count;

    private int WindowRingPos => ProcessedSamples % WindowSize;

    public void SetStripeAddedCallback(Action callback)
    {
        if (_stripeAddedCallback != null)
        {
            throw new InvalidOperationException();
        }

        _stripeAddedCallback = callback;
    }

    public void AddChunk(float[] chunk)
    {
        if (chunk.Length != ChunkSize)
        {
            throw new Exception();
        }

        Array.Copy(chunk, 0, _windowRing, WindowRingPos, ChunkSize);

        ProcessedSamples += ChunkSize;

        if (ProcessedSamples >= WindowSize)
        {
            AddStripe();
        }
    }

    private void AddStripe()
    {
        for (var i = 0; i < WindowSize; i++)
        {
            var waveRingIndex = (WindowRingPos + i) % WindowSize;
            _fftBuf[i] = new Complex32(_windowRing[waveRingIndex] * Hann[i], 0);
        }

        Fourier.Forward(_fftBuf, FourierOptions.NoScaling);

        var stripe = new float[BinCount];
        for (var bin = 0; bin < BinCount; bin++)
        {
            // Used in official Shazam since 7.11.0
            // https://github.com/marin-m/SongRec/issues/10#issuecomment-731527377
            const int scaling = 2;

            stripe[bin] = scaling * _fftBuf[bin].MagnitudeSquared;
        }

        _stripes.Add(stripe);

        _stripeAddedCallback?.Invoke();
    }

    public float GetMagnitudeSquared(int stripe, int bin)
    {
        return _stripes[stripe][bin];
    }

    public float FindMaxMagnitudeSquared()
    {
        return _stripes.Max(s => s.Max());
    }

    public static int FreqToBin(float freq)
    {
        return Convert.ToInt32(freq * WindowSize / SampleRate);
    }

    public static float BinToFreq(float bin)
    {
        return bin * SampleRate / WindowSize;
    }
}