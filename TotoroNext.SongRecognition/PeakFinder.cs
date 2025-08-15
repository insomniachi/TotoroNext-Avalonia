namespace TotoroNext.SongRecognition;

internal record PeakInfo(
    int StripeIndex,
    float InterpolatedBin,
    float LogMagnitude
);

internal class PeakFinder
{
    public const int
        // Refer to Test/PeakDensityResearch.Fat()
        HStripeDist = 45,
        HBinDist = 1,
        VStripeDist = 3,
        VBinDist = 10;

    private const float MinMagnSquared = 1f / 512 / 512;

    private static readonly IReadOnlyList<int> BandFreqs = [250, 520, 1450, 3500, 5500];

    private static readonly int
        MinBin = Math.Max(Analysis.FreqToBin(BandFreqs.Min()), VBinDist),
        MaxBin = Math.Min(Analysis.FreqToBin(BandFreqs.Max()), Analysis.BinCount - VBinDist);

    private static readonly float
        LogMinMagnSquared = MathF.Log(MinMagnSquared);

    private readonly Analysis _analysis;
    private readonly IReadOnlyList<List<PeakInfo>> _bands;
    private readonly bool _interpolation;

    public PeakFinder(Analysis analysis, bool interpolation = true)
    {
        analysis.SetStripeAddedCallback(Analysis_StripeAddedCallback);

        _analysis = analysis;
        _interpolation = interpolation;

        _bands = Enumerable.Range(0, BandFreqs.Count - 1)
                           .Select(_ => new List<PeakInfo>())
                           .ToList();
    }

    private void Analysis_StripeAddedCallback()
    {
        if (_analysis.StripeCount > 2 * HStripeDist)
        {
            Find(_analysis.StripeCount - HStripeDist - 1);
        }
    }

    private void Find(int stripe)
    {
        for (var bin = MinBin; bin < MaxBin; bin++)
        {
            if (_analysis.GetMagnitudeSquared(stripe, bin) < MinMagnSquared)
            {
                continue;
            }

            if (!IsPeak(stripe, bin, HStripeDist, HBinDist))
            {
                continue;
            }

            if (!IsPeak(stripe, bin, VStripeDist, VBinDist))
            {
                continue;
            }

            AddPeakAt(stripe, bin);
        }
    }

    public IEnumerable<IEnumerable<PeakInfo>> EnumerateBandedPeaks()
    {
        return _bands;
    }

    public IEnumerable<PeakInfo> EnumerateAllPeaks()
    {
        return _bands.SelectMany(i => i);
    }

    public void ApplyRateLimit()
    {
        // Derived by comparison with official signature
        // StripeCount / 11 also works
        var allowedCount = 12 + _analysis.StripeCount / 12;

        foreach (var peakList in _bands)
        {
            if (peakList.Count <= allowedCount)
            {
                continue;
            }

            peakList.Sort((x, y) => -Comparer<float>.Default.Compare(x.LogMagnitude, y.LogMagnitude));
            peakList.RemoveRange(allowedCount, peakList.Count - allowedCount);
            peakList.Sort((x, y) => Comparer<int>.Default.Compare(x.StripeIndex, y.StripeIndex));
        }
    }

    private int GetBandIndex(float bin)
    {
        var freq = Analysis.BinToFreq(bin);

        if (freq < BandFreqs[0])
        {
            return -1;
        }

        for (var i = 1; i < BandFreqs.Count; i++)
        {
            if (freq < BandFreqs[i])
            {
                return i - 1;
            }
        }

        return -1;
    }

    private PeakInfo CreatePeakAt(int stripe, int bin)
    {
        if (!_interpolation)
        {
            return new PeakInfo(stripe, bin, GetLogMagnitude(stripe, bin));
        }

        // Quadratic Interpolation of Spectral Peaks
        // https://stackoverflow.com/a/59140547
        // https://ccrma.stanford.edu/~jos/sasp/Quadratic_Interpolation_Spectral_Peaks.html

        // https://ccrma.stanford.edu/~jos/parshl/Peak_Detection_Steps_3.html
        // "We have found empirically that the frequencies tend to be about twice as accurate"
        // "when dB magnitude is used rather than just linear magnitude"

        var alpha = GetLogMagnitude(stripe, bin - 1);
        var beta = GetLogMagnitude(stripe, bin);
        var gamma = GetLogMagnitude(stripe, bin + 1);
        var p = (alpha - gamma) / (alpha - 2 * beta + gamma) / 2;

        return new PeakInfo(
                            stripe,
                            bin + p,
                            beta // - (alpha - gamma) * p / 4
                           );
    }

    private float GetLogMagnitude(int stripe, int bin)
    {
        return 18 * 1024 * (1 - MathF.Log(_analysis.GetMagnitudeSquared(stripe, bin)) / LogMinMagnSquared);
    }

    private bool IsPeak(int stripe, int bin, int stripeDist, int binDist)
    {
        var center = _analysis.GetMagnitudeSquared(stripe, bin);
        for (var s = -stripeDist; s <= stripeDist; s++)
        {
            for (var b = -binDist; b <= binDist; b++)
            {
                if (s == 0 && b == 0)
                {
                    continue;
                }

                if (_analysis.GetMagnitudeSquared(stripe + s, bin + b) >= center)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void AddPeakAt(int stripe, int bin)
    {
        var newPeak = CreatePeakAt(stripe, bin);

        var bandIndex = GetBandIndex(newPeak.InterpolatedBin);
        if (bandIndex < 0)
        {
            return;
        }

        _bands[bandIndex].Add(newPeak);
    }
}