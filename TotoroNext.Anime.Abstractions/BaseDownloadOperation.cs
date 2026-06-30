using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TotoroNext.Anime.Abstractions;

public abstract partial class BaseDownloadOperation : ObservableObject, IDownloadOperation
{
    [ObservableProperty] public partial double Progress { get; set; }
    [ObservableProperty] public partial bool DownloadStarted { get; protected set; }
    [ObservableProperty] public partial double Speed { get; set; }
    [ObservableProperty] public partial long DownloadedBytes { get; set; }
    [ObservableProperty] public partial long TotalBytes { get; set; }
    [ObservableProperty] public partial bool IsCompleted { get; protected set; }
    [ObservableProperty] public partial bool IsPaused { get; set; }
    [ObservableProperty] public partial bool IsCancelled { get; set; }
    public required Uri Link { get; init; }
    public required string FileName { get; init; }
    public abstract Task StartAsync();
    
    [RelayCommand]
    private void TogglePauseResume()
    {
        TogglePauseResumeImpl();
    }

    [RelayCommand]
    private void Cancel()
    {
        CancelImpl();
    }

    protected abstract void TogglePauseResumeImpl();
    protected abstract void CancelImpl();
}