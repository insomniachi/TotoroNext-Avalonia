using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Humanizer;

namespace TotoroNext.Module.Converters;

public static class Converters
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

    public static readonly IValueConverter ByteToStringConverter = new FuncValueConverter<long, string>(b => ByteSize.FromBytes(b).Humanize());

    public static readonly IValueConverter DownloadSpeedConverter =
        new FuncValueConverter<double, string>(b => ByteSize.FromBytes(b).Per(OneSecond).Humanize());
    
    public static readonly IValueConverter DownloadSpeedConverterLong = 
        new FuncValueConverter<long?, string>(b => ByteSize.FromBytes(b ?? 0).Per(OneSecond).Humanize());

    public static readonly IValueConverter TimeSpanConverter = new FuncValueConverter<TimeSpan, string>(ts => ts.Hours > 0
             ? ts.ToString(@"hh\:mm\:ss")
             : ts.ToString(@"mm\:ss"));

    public static readonly IValueConverter IconConverter = new FuncValueConverter<string, PathIcon>(IconRegistry.GetPathIcon!);
    
    public static readonly IValueConverter BoolToBackgroundConverter = new FuncValueConverter<bool, IBrush>(isSelected =>
        isSelected ? new SolidColorBrush(Color.Parse("#1E90FF")) : Brushes.Transparent);
    
    public static readonly IValueConverter BoolToOpacityConverter = new FuncValueConverter<bool, double>(isSelected =>
        isSelected ? 1.0 : 0.7);
    
    public static readonly IValueConverter BoolToFontWeightConverter = new FuncValueConverter<bool, FontWeight>(isSelected =>
        isSelected ? FontWeight.Bold : FontWeight.Normal);
}