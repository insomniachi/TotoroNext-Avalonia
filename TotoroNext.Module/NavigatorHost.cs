using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public interface IInitializable
{
    void Initialize();
}

public interface IAsyncInitializable
{
    Task InitializeAsync();
}

public class NavigatorHost(
    TransitioningContentControl host,
    IViewRegistry locator,
    ILogger<NavigatorHost> logger,
    IServiceScopeFactory serviceScopeFactory) : INavigator
{
    private TransitioningContentControl Control { get; } = host;
    public event EventHandler<NavigationResult>? Navigated;

    public bool NavigateToData(object data)
    {
        try
        {
            var map = locator.FindByData(data.GetType());

            if (map is not { View: { } viewType, ViewModel: { } vmType })
            {
                return false;
            }

            var view = (StyledElement)Activator.CreateInstance(viewType)!;
            using var scope = serviceScopeFactory.CreateScope();
            var vmObj = ActivatorUtilities.CreateInstance(scope.ServiceProvider, vmType, data);

            NavigationExtensions.ConfigureView(view, vmObj);
            Navigate(view);
            Navigated?.Invoke(this, new NavigationResult(viewType, vmType));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Navigation failed");
            return false;
        }
    }

    public bool NavigateToRoute(string path)
    {
        try
        {
            var map = locator.FindByKey(path);

            if (map is not { View: { } viewType, ViewModel: { } vmType })
            {
                return false;
            }

            var view = (StyledElement)Activator.CreateInstance(viewType)!;
            using var scope = serviceScopeFactory.CreateScope();
            var vmObj = ActivatorUtilities.CreateInstance(scope.ServiceProvider, vmType);

            NavigationExtensions.ConfigureView(view, vmObj);
            Navigate(view);
            Navigated?.Invoke(this, new NavigationResult(viewType, vmType));

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Navigation failed");
            return true;
        }
    }

    public bool NavigateViewModel(Type vmType)
    {
        try
        {
            var map = locator.FindByViewModel(vmType);

            if (map is not { View: { } viewType })
            {
                return false;
            }

            var view = (StyledElement)Activator.CreateInstance(viewType)!;
            using var scope = serviceScopeFactory.CreateScope();
            var vmObj = ActivatorUtilities.CreateInstance(scope.ServiceProvider, vmType);

            NavigationExtensions.ConfigureView(view, vmObj);
            Navigate(view);
            Navigated?.Invoke(this, new NavigationResult(viewType, vmType));

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Navigation failed");
            return false;
        }
    }

    private void Navigate(StyledElement page)
    {
        Control.Content = page;
    }
}