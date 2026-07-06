using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TotoroNext.Anime.Abstractions;

public interface IDownloadOperation : INotifyPropertyChanged
{
    double Progress { get; set; }
    bool DownloadStarted { get; }
    double Speed { get; set; }
    long DownloadedBytes { get; set; }
    long TotalBytes { get; set; }
    bool IsCompleted { get; }
    bool IsPaused { get; set; }
    bool IsCancelled { get; set; }
    Uri Link { get; init; }
    string FileName { get; init; }
    IRelayCommand TogglePauseResumeCommand { get; }
    IRelayCommand CancelCommand { get; }
    Task StartAsync();
    event EventHandler? Completed;
    event EventHandler? Started;
}