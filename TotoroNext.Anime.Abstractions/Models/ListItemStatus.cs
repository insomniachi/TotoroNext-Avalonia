using System.ComponentModel;

namespace TotoroNext.Anime.Abstractions.Models;

public enum ListItemStatus
{
    [Description("Watching")] Watching,

    [Description("Completed")] Completed,

    [Description("On Hold")] OnHold,

    [Description("Planning")] PlanToWatch,

    [Description("Dropped")] Dropped,

    [Description("Rewatching")] Rewatching,
}