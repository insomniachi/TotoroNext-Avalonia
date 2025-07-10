using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
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

public class NavigatorHost(TransitioningContentControl host,
                           IViewRegistry locator,
                           IServiceScopeFactory serviceScopeFactory) : INavigator
{
    public event EventHandler<NavigationResult>? Navigated;

    private TransitioningContentControl Control { get; } = host;

    public bool NavigateToData(object data)
    {
        try
        {
            var map = locator.FindByData(data.GetType());

            if (map is not { View: { } viewType, ViewModel: { } vmType })
            {
                return false;
            }

            var page = (StyledElement)Activator.CreateInstance(viewType)!;
            using var scope = serviceScopeFactory.CreateScope();
            var vmObj = ActivatorUtilities.CreateInstance(scope.ServiceProvider, vmType, data);

            ConfigurePage(page, vmObj);
            Navigate(page);
            Navigated?.Invoke(this, new(viewType, vmType));
            return true;
        }
        catch
        {
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

            var page = (StyledElement)Activator.CreateInstance(viewType)!;
            using var scope = serviceScopeFactory.CreateScope();
            var vmObj = ActivatorUtilities.CreateInstance(scope.ServiceProvider, vmType);

            ConfigurePage(page, vmObj);
            Navigate(page);
            Navigated?.Invoke(this, new(viewType, vmType));

            return true;
        }
        catch
        {
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

            var page = (StyledElement)Activator.CreateInstance(viewType)!;
            using var scope = serviceScopeFactory.CreateScope();
            var vmObj = ActivatorUtilities.CreateInstance(scope.ServiceProvider, vmType);

            ConfigurePage(page, vmObj);
            Navigate(page);
            Navigated?.Invoke(this, new(viewType, vmType));

            return true;
        }
        catch(Exception ex)
        {
            return false;
        }
    }

    private void Navigate(StyledElement page)
    {
        Control.Content = page;
    }

    private static void ConfigurePage(StyledElement page, object vm)
    {
        page.DataContext = vm;
        page.AttachedToLogicalTree += async (_, _) =>
        {
            switch (vm)
            {
                case IInitializable { } i:
                    i.Initialize();
                    break;
                case IAsyncInitializable { } ia:
                    try
                    {
                        await ia.InitializeAsync();
                    }
                    catch(Exception ex)
                    {
                        throw;
                    }

                    break;
            }
        };
        page.DetachedFromLogicalTree += async (_, _) =>
        {
            if (vm is IDisposable d)
            {
                d.Dispose();
            }
            if (vm is IAsyncDisposable ad)
            {
                await ad.DisposeAsync();
            }
        };
    }
}