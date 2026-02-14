using System.Text.Json;
using System.Text.Json.Serialization;
using Flurl;
using Flurl.Http;
using TotoroNext.Module;

namespace TotoroNext.Anime.SubsPlease;

internal static class Schedule
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    internal static List<NextEpisodeInfo> Items { get; set; } = [];
    
    public static async Task DownloadSchedule()
    {
        Items.Clear();
        
        var response = await "https://subsplease.org/api"
                             .AppendQueryParam("f", "schedule")
                             .AppendQueryParam("tz", "Etc/GMT")
                             .GetStreamAsync();

        var schedule = await JsonSerializer.DeserializeAsync<ScheduleResult>(response);

        if (schedule is null)
        {
            return;
        }
        
        var file = FileHelper.GetModulePath(Module.Descriptor, "schedule.json");
        var directory = Path.GetDirectoryName(file);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(schedule, Options));
        
        Initialize(schedule);
    }
    
    public static void Initialize(ScheduleResult schedule)
    {
        AddScheduleItems(DayOfWeek.Monday, schedule.Schedule.Monday);
        AddScheduleItems(DayOfWeek.Tuesday, schedule.Schedule.Tuesday);
        AddScheduleItems(DayOfWeek.Wednesday, schedule.Schedule.Wednesday);
        AddScheduleItems(DayOfWeek.Thursday, schedule.Schedule.Thursday);
        AddScheduleItems(DayOfWeek.Friday, schedule.Schedule.Friday);
        AddScheduleItems(DayOfWeek.Saturday, schedule.Schedule.Saturday);
        AddScheduleItems(DayOfWeek.Sunday, schedule.Schedule.Sunday);
    }
    
    private static void AddScheduleItems(DayOfWeek dayOfWeek, List<ScheduleItem> items)
    {
        foreach (var item in items)
        {
            var airingTime = ConvertToNextAiringTime(dayOfWeek, item);
            Items.Add(new NextEpisodeInfo()
            {
                Title = item.Title,
                Id = item.Page,
                AirsAt =  airingTime,
            });
        }
    }
    
    private static DateTimeOffset ConvertToNextAiringTime(DayOfWeek dayOfWeek, ScheduleItem item)
    {
        // Parse the time (HH:mm format)
        var timeParts = item.Time.Split(':');
        var hour = int.Parse(timeParts[0]);
        var minute = int.Parse(timeParts[1]);

        // Get today's date
        var today = DateTime.Today;
        var currentDayOfWeek = today.DayOfWeek;
        
        // Calculate days until target day
        var daysUntilTarget = ((int)dayOfWeek - (int)currentDayOfWeek + 7) % 7;
        if (daysUntilTarget == 0 && DateTime.UtcNow.TimeOfDay > new TimeSpan(hour, minute, 0))
        {
            daysUntilTarget = 7; // If that day already passed today, get next week's date
        }
        
        // Calculate the airing date
        var airingDate = today.AddDays(daysUntilTarget);
        var gmtTime = new DateTime(airingDate.Year, airingDate.Month, airingDate.Day, hour, minute, 0, DateTimeKind.Utc);
        
        // Convert GMT to local timezone
        var localTime = TimeZoneInfo.ConvertTime(gmtTime, TimeZoneInfo.Utc, TimeZoneInfo.Local);
        
        return new DateTimeOffset(localTime, TimeZoneInfo.Local.GetUtcOffset(gmtTime));
    }
}

internal class NextEpisodeInfo
{
    public required string Title { get; init; }
    public required string Id { get; init; }
    public required DateTimeOffset AirsAt { get; init; }
}

[Serializable]
public class ScheduleResult
{
    [JsonPropertyName("tz")] public required string TimeZone { get; set; }

    [JsonPropertyName("schedule")] public WeeklySchedule Schedule { get; set; } = new();
}

[Serializable]
public class WeeklySchedule
{
    public List<ScheduleItem> Monday { get; set; } = [];
    public List<ScheduleItem> Tuesday { get; set; } = [];
    public List<ScheduleItem> Wednesday { get; set; } = [];
    public List<ScheduleItem> Thursday { get; set; } = [];
    public List<ScheduleItem> Friday { get; set; } = [];
    public List<ScheduleItem> Saturday { get; set; } = [];
    public List<ScheduleItem> Sunday { get; set; } = [];
}

[Serializable]
public class ScheduleItem
{
    [JsonPropertyName("page")] public required string Page { get; set; }

    [JsonPropertyName("title")] public required string Title { get; set; }

    [JsonPropertyName("time")] public required string Time { get; set; }
}