using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public abstract partial class BaseDownloadOperation(AnimeModel anime, Episode ep) : ObservableObject, IDownloadOperation
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
    public AnimeModel Anime => anime;
    public Episode Episode => ep;
    public abstract Task StartAsync();
    public event EventHandler? Completed;
    public event EventHandler? Started;

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
    protected void OnCompleted() => Completed?.Invoke(this, EventArgs.Empty);
    protected void OnStarted() => Started?.Invoke(this, EventArgs.Empty);
}