using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

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

    public async Task<IDownloadOperation?> CreateDownload(AnimeModel anime, Episode episode, VideoSource source, string filepath)
    {
        if (string.IsNullOrEmpty(_executable))
        {
            return null;
        }
        

        return new YtdlpDownloadOperation(source, filepath)
        {
            Link = source.Url,
            FileName = filepath,
            YtdlpPath = _executable
        };
    }
}