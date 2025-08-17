using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using TotoroNext.Anime.Abstractions;
using TotoroNext.Module;

namespace TotoroNext.Anime.Anilist.ViewModels;

[UsedImplicitly]
public partial class AiringScheduleViewModel(IAnilistMetadataService metadataService) : ObservableObject, IAsyncInitializable
{
    [ObservableProperty] public partial List<AnimeModel> Monday { get; set; } = [];
    [ObservableProperty] public partial List<AnimeModel> Tuesday { get; set; } = [];
    [ObservableProperty] public partial List<AnimeModel> Wednesday { get; set; } = [];
    [ObservableProperty] public partial List<AnimeModel> Thursday { get; set; } = [];
    [ObservableProperty] public partial List<AnimeModel> Friday { get; set; } = [];
    [ObservableProperty] public partial List<AnimeModel> Saturday { get; set; } = [];
    [ObservableProperty] public partial List<AnimeModel> Sunday { get; set; } = [];
    
    public DayOfWeek Today { get; } = DateTime.Now.DayOfWeek;

    public async Task InitializeAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var daysToMonday = ((int)now.DayOfWeek + 6) % 7; // Sunday = 0, Monday = 1
        var monday = now.Date.AddDays(-daysToMonday);
        var sunday = monday.AddDays(6).AddDays(1).AddTicks(-1);
        var start = (int)new DateTimeOffset(monday).ToUnixTimeSeconds();
        var end = (int)new DateTimeOffset(sunday).ToUnixTimeSeconds();

        var schedule = await metadataService.GetAiringSchedule(start, end);
        Monday = [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Monday).Select(x => x.Anime)];
        Tuesday = [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Tuesday).Select(x => x.Anime)];
        Wednesday = [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Wednesday).Select(x => x.Anime)];
        Thursday = [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Thursday).Select(x => x.Anime)];
        Friday = [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Friday).Select(x => x.Anime)];
        Saturday = [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Saturday).Select(x => x.Anime)];
        Sunday = [..schedule.Where(x => x.Start.DayOfWeek == DayOfWeek.Sunday).Select(x => x.Anime)];
    }
}