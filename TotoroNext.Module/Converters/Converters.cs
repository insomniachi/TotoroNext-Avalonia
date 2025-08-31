using Avalonia.Data.Converters;
using Humanizer;
using Humanizer.Bytes;

namespace TotoroNext.Module.Converters;

public class Converters
{
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
    
    public static readonly IValueConverter ByteToStringConverter = new FuncValueConverter<long, string>(b => ByteSize.FromBytes(b).Humanize());
    public static readonly IValueConverter DownloadSpeedConverter = new FuncValueConverter<double, string>(b => ByteSize.FromBytes(b).Per(OneSecond).Humanize());
}