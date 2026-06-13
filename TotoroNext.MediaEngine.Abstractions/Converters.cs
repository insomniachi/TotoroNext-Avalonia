using Avalonia.Controls;
using Avalonia.Data.Converters;
using TotoroNext.Module;

namespace TotoroNext.MediaEngine.Abstractions;

public static class Converters
{
    public static IValueConverter MediaPlayerStateToIconConverter { get; } =
        new FuncValueConverter<MediaPlayerState, PathIcon>(state => state switch
        {
            MediaPlayerState.Playing => IconRegistry.GetPathIcon(CommonIcons.Pause),
            _ => IconRegistry.GetPathIcon(CommonIcons.Play)
        });
}