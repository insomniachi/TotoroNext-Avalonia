using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace TotoroNext.Module;

public static class UnsafeAccessors
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_dialogResult")]
    public static extern ref object? DialogResult(Window window);
}