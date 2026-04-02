using Avalonia.Data.Converters;

namespace TotoroNext.Torrents.Abstractions.Converters;

public static class TorrentConverters
{
    public static readonly IValueConverter PercentageConverter = 
        new FuncValueConverter<double, string>(value => $"{value * 100:F1}%");
}

