using System.Runtime.ExceptionServices;
using TotoroNext.SongRecognition.Models;

namespace TotoroNext.SongRecognition;

internal static class CaptureAndTag
{
    private static readonly float[] Chunk = new float[Analysis.ChunkSize];

    public static async Task<ShazamResult?> RunAsync(ICaptureHelper captureHelper)
    {
        var analysis = new Analysis();
        var finder = new PeakFinder(analysis);

        var retryMs = 3000;
        var tagId = Guid.NewGuid().ToString();

        while (true)
        {
            var readChunkResult = await ReadChunkAsync(captureHelper);

            if (readChunkResult == ReadChunkResult.Eof)
            {
                return null;
            }

            if (readChunkResult == ReadChunkResult.SampleProviderChanged)
            {
                analysis = new Analysis();
                finder = new PeakFinder(analysis);
                continue;
            }

            analysis.AddChunk(Chunk);

            if (analysis.ProcessedMs < retryMs)
            {
                continue;
            }

            var sigBytes = Sig.Write(Analysis.SampleRate, analysis.ProcessedSamples, finder);
            var result = await ShazamApi.SendRequestAsync(tagId, analysis.ProcessedMs, sigBytes);
            if (result.Success)
            {
                return result;
            }

            retryMs = result.RetryMs;
            if (retryMs == 0)
            {
                return result;
            }
        }
    }

    private static async Task<ReadChunkResult> ReadChunkAsync(ICaptureHelper captureHelper)
    {
        var sampleProvider = captureHelper.SampleProvider;
        var offset = 0;
        var expectedCount = Chunk.Length;

        while (true)
        {
            if (captureHelper.Exception != null)
            {
                ExceptionDispatchInfo.Capture(captureHelper.Exception).Throw();
            }

            if (captureHelper.SampleProvider != sampleProvider)
            {
                return ReadChunkResult.SampleProviderChanged;
            }

            if (sampleProvider != null)
            {
                var actualCount = sampleProvider.Read(Chunk, offset, expectedCount);

                if (actualCount == expectedCount)
                {
                    return ReadChunkResult.Ok;
                }

                if (!captureHelper.Live)
                {
                    return ReadChunkResult.Eof;
                }

                offset += actualCount;
                expectedCount -= actualCount;
            }

            await Task.Delay(100);
        }
    }

    private enum ReadChunkResult
    {
        Ok,
        SampleProviderChanged,
        Eof
    }
}