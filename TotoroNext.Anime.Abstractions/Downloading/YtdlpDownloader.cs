using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions.Downloading;

public class YtdlpDownloader : IDownloader
{
    private readonly string? _executable;

    public YtdlpDownloader(ILocalSettingsService localSettingsService,
                           IDialogService dialogService)
    {
        _executable = localSettingsService.ReadSetting<string>("YtdlpPath", "");
        if (string.IsNullOrEmpty(_executable))
        {
            dialogService.Warning("Yt-dlp  path is missing, configure in settings");
        }
    }

    public Task<IDownloadOperation?> CreateDownload(AnimeModel anime, Episode episode, VideoSource source, string filepath)
    {
        try
        {
            if (string.IsNullOrEmpty(_executable))
            {
                return Task.FromResult<IDownloadOperation?>(null);
            }

            return Task.FromResult<IDownloadOperation?>(new YtdlpDownloadOperation(source, filepath, anime, episode)
            {
                Link = source.Url,
                FileName = filepath,
                YtdlpPath = _executable
            });
        }
        catch (Exception exception)
        {
            return Task.FromException<IDownloadOperation?>(exception);
        }
    }
}