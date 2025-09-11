using Avalonia.Data.Converters;
using IconPacks.Avalonia.MaterialDesign;

namespace TotoroNext.MediaEngine.Abstractions;

public static class Converters
{
    public static IValueConverter MediaPlayerStateToIconConverter { get; } = new FuncValueConverter<MediaPlayerState, PackIconMaterialDesignKind>(state => state switch
    {
        MediaPlayerState.Playing => PackIconMaterialDesignKind.Pause,
        _ => PackIconMaterialDesignKind.PlayArrow
    });
}