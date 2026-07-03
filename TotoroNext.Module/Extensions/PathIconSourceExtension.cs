using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace TotoroNext.Module.Extensions;

public class PathIconSourceExtension : MarkupExtension
{
    public object? Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Key is string keyString)
        {
            return IconRegistry.GetPathIconSource(keyString) ?? AvaloniaProperty.UnsetValue;
        }
        return AvaloniaProperty.UnsetValue;
    }
}