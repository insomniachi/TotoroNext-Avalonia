namespace TotoroNext.MediaEngine.Abstractions;

public interface IMediaSegmentsProvider
{
    Task<List<MediaSegment>> GetSegments(long id, float episode, double episodeLength);
}
