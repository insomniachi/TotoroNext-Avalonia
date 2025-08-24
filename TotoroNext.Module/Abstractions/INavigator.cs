using Ursa.Controls;

namespace TotoroNext.Module.Abstractions;

public interface INavigator
{
    event EventHandler<NavigationResult>? Navigated;
    bool NavigateViewModel(Type vmType);
    bool NavigateToData(object data);
    bool NavigateToRoute(string path);
}

public interface INavigatorHost
{
    INavigator? Navigator { get; set; }
}

public class NavigateToViewModelMessage(Type vm)
{
    public Type ViewModel { get; } = vm;
}

public class NavigateToDialogMessage
{
    public required string Title { get; init; }
    public object? Data { get; init; }
    public DialogButton Button { get; init; } = DialogButton.None;
    public bool CloseButtonVisible { get; init; }
}

public class NavigateToViewModelDialogMessage : NavigateToDialogMessage
{
    public required Type ViewModel { get; init; }
}

public class NavigateToKeyDialogMessage : NavigateToDialogMessage
{
    public required string Key { get; init; }
}

public class PaneNavigateToViewModelMessage(Type vm, string title = "", double? paneWidth = null, bool isInline = false)
    : NavigateToViewModelMessage(vm)
{
    public string Title { get; } = title;
    public double? PaneWidth { get; } = paneWidth;
    public bool IsInline { get; } = isInline;
}

public class NavigateToDataMessage(object data)
{
    public object Data { get; } = data;
}

public class PaneNavigateToDataMessage(object data, string title = "", double? paneWidth = null, bool isInline = false) : NavigateToDataMessage(data)
{
    public string Title { get; } = title;
    public double? PaneWidth { get; } = paneWidth;
    public bool IsInline { get; } = isInline;
}

public class NavigateToRouteMessage(string path)
{
    public string Path { get; } = path;
}

public record NavigationResult(Type ViewType, Type ViewModelType);