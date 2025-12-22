using Avalonia.Data.Converters;
using TotoroNext.Anime.Abstractions.Models;

namespace TotoroNext.Anime.Abstractions;

public static class Converters
{
    public static IValueConverter HasAiredConverter { get; } =
        new FuncValueConverter<Models.AnimeModel, bool>(d => d?.AiringStatus is not AiringStatus.NotYetAired);
    
    public static IValueConverter HtmlToTextConverter { get; } = new FuncValueConverter<string, string>(html =>
    {
        if (string.IsNullOrEmpty(html))
        {
            return "";
        }

        return html.Replace("<i>", "")
                   .Replace("</i>", "")
                   .Replace("<b>", "")
                   .Replace("</b>", "")
                   .Replace("<br><br>", Environment.NewLine);
    });
}