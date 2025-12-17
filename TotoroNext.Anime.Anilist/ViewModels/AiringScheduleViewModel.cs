using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;

namespace TotoroNext.Anime.Anilist.ViewModels;

[UsedImplicitly]
public sealed partial class AiringScheduleViewModel(IAnilistMetadataService metadataService) : ObservableObject, IAsyncInitializable, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial List<List<AnimeModel>> Schedule { get; set; } = [];

    public DayOfWeek Today { get; } = DateTime.Now.DayOfWeek;

    public async Task InitializeAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var daysToMonday = ((int)now.DayOfWeek + 6) % 7; // Sunday = 0, Monday = 1
        var monday = now.Date.AddDays(-daysToMonday);
        var sunday = monday.AddDays(6).AddDays(1).AddTicks(-1);
        var start = (int)new DateTimeOffset(monday).ToUnixTimeSeconds();
        var end = (int)new DateTimeOffset(sunday).ToUnixTimeSeconds();

        IsLoading = true;

        var schedule = await metadataService.GetAiringSchedule(start, end, _cts.Token);

        Schedule =
        [
            [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Monday).Select(x => x.Anime)],
            [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Tuesday).Select(x => x.Anime)],
            [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Wednesday).Select(x => x.Anime)],
            [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Thursday).Select(x => x.Anime)],
            [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Friday).Select(x => x.Anime)],
            [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Saturday).Select(x => x.Anime)],
            [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Sunday).Select(x => x.Anime)]
        ];

        IsLoading = false;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}