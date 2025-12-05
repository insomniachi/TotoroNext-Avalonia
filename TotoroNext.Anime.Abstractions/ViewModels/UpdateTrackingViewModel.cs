using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Anime.Abstractions.ViewModels;

[UsedImplicitly]
public partial class UpdateTrackingViewModel(
    AnimeModel anime,
    ITrackingUpdater trackingUpdater) : ObservableObject, IDialogViewModel
{
    public AnimeModel Anime { get; } = anime;

    [ObservableProperty] public partial ListItemStatus? Status { get; set; } = anime.Tracking?.Status;

    [ObservableProperty] public partial int? Progress { get; set; } = anime.Tracking?.WatchedEpisodes;

    [ObservableProperty] public partial int? Score { get; set; } = anime.Tracking?.Score;

    [ObservableProperty]
    public partial DateTime? StartDate { get; set; } = anime.Tracking?.StartDate == new DateTime() ? null : anime.Tracking?.StartDate;

    [ObservableProperty]
    public partial DateTime? FinishDate { get; set; } = anime.Tracking?.FinishDate == new DateTime() ? null : anime.Tracking?.FinishDate;

    public Action? Close { get; set; }

    public async Task Handle(DialogResult result)
    {
        if (result is not DialogResult.OK)
        {
            return;
        }

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