using Avalonia.Markup.Xaml;

namespace TotoroNext.Module.Extensions;

public class PathIconExtension : MarkupExtension
{
    public string? Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Key);
        return IconRegistry.GetPathIcon(Key);
    }
}