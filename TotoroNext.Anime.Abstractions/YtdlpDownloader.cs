using TotoroNext.Anime.Abstractions.Models;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Abstractions;

public class YtdlpDownloader : BaseDownloader
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
    
    protected override IDownloadOperation? CreateDownload(AnimeModel anime, Episode episode, VideoServer server, string filepath)
    {
        if (string.IsNullOrEmpty(_executable))
        {
            return null;
        }
        
        return new YtdlpDownloadOperation(server, filepath)
        {
            Link = server.Url,
            FileName = filepath,
            YtdlpPath = _executable
        };
    }
}