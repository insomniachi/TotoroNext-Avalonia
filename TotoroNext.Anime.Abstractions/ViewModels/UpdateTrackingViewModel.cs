using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions.Models;
using Ursa.Controls;

namespace TotoroNext.Anime.Abstractions.ViewModels;

[UsedImplicitly]
public partial class UpdateTrackingViewModel(
    AnimeModel anime,
    ITrackingUpdater trackingUpdater) : ObservableObject, IDialogContext
{
    public AnimeModel Anime { get; } = anime;

    [ObservableProperty] public partial ListItemStatus? Status { get; set; } = anime.Tracking?.Status;

    [ObservableProperty] public partial int? Progress { get; set; } = anime.Tracking?.WatchedEpisodes;

    [ObservableProperty] public partial int? Score { get; set; } = anime.Tracking?.Score;

    [ObservableProperty]
    public partial DateTime? StartDate { get; set; } = anime.Tracking?.StartDate == new DateTime() ? null : anime.Tracking?.StartDate;

    [ObservableProperty]
    public partial DateTime? FinishDate { get; set; } = anime.Tracking?.FinishDate == new DateTime() ? null : anime.Tracking?.FinishDate;

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(this, DialogResult.OK);
    }

    public event EventHandler<object?>? RequestClose;

    [RelayCommand]
    private async Task UpdateTracking()
    {
        var tracking = new Tracking
        {
            Status = Status,
            WatchedEpisodes = Progress,
            Score = Score,
            StartDate = StartDate,
            FinishDate = FinishDate
        };

        if (AreEqual(tracking, Anime.Tracking))
        {
            return;
        }

        await trackingUpdater.UpdateTracking(Anime, tracking);
        Close();
    }

    [RelayCommand]
    private async Task DeleteTracking()
    {
        await trackingUpdater.RemoveTracking(Anime);
        Close();
    }

    private static bool AreEqual(Tracking? left, Tracking? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Status == right.Status &&
               left.WatchedEpisodes == right.WatchedEpisodes &&
               left.Score == right.Score &&
               left.StartDate == right.StartDate &&
               left.FinishDate == right.FinishDate;
    }
}