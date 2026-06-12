using Avalonia.Markup.Xaml;

namespace TotoroNext.Module.Extensions;

public class PathIconSourceExtension : MarkupExtension
{
    public string? Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Key);
        return IconRegistry.GetPathIconSource(Key);
    }
}