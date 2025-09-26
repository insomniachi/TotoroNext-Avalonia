using Avalonia.Data.Converters;

namespace TotoroNext.Anime.Abstractions;

public static class Converters
{
    public static IValueConverter HasAiredConverter { get; } = new FuncValueConverter<AnimeModel, bool>(d => d?.AiringStatus is not AiringStatus.NotYetAired);
}