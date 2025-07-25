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

public class ClosePaneMessage;

public class PaneClosingMessage;

public class ToggleAppWindowPresenterMessage;

public class FullScreenEntered;

public class FullScreenExited;