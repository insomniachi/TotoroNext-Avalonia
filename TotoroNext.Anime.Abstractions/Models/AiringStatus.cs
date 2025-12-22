using System.ComponentModel;

namespace TotoroNext.Anime.Abstractions.Models;

public enum AiringStatus
{
    [Description("Finished Airing")] FinishedAiring,

    [Description("Currently Airing")] CurrentlyAiring,

    [Description("Not Yet Aired")] NotYetAired
}