using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Module;

public partial class Selectable<T>(T value) : ObservableObject
{
    [ObservableProperty] public partial bool IsSelected { get; set; }
    public T Value { get; } = value;
}