using System.Reactive.Linq;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace TotoroNext.ViewModels;

public partial class MainWindowViewModel : ObservableObject,
                                           INavigatorHost,
                                           IRecipient<NavigateToViewModelMessage>,
                                           IRecipient<NavigateToDataMessage>,
                                           IRecipient<PaneNavigateToViewModelMessage>,
                                           IRecipient<PaneNavigateToDataMessage>
{
    private readonly IViewRegistry _locator;

    public MainWindowViewModel(IEnumerable<NavMenuItem> menuItems,
                               IViewRegistry locator,
                               IMessenger messenger)
    {
        _locator = locator;
        var items = menuItems.OrderBy(x => x.Tag is not NavMenuItemTag tag ? 0 : tag.Order).ToList();
        MapChildren(items);
        MenuItems = [..items.Where(x => IsTopLevelNavItem(x) && !IsFooterNavItem(x))];
        FooterMenuItems = [..items.Where(x => IsTopLevelNavItem(x) && IsFooterNavItem(x))];

        this.WhenAnyValue(x => x.Navigator)
            .WhereNotNull()
            .FirstAsync()
            .Subscribe(navigator =>
            {
                navigator.Navigated += (_, args) => { UpdateSelection(args); };
                navigator.NavigateToRoute("Anime List");
            });

        messenger.RegisterAll(this);
    }

    public List<NavMenuItem> MenuItems { get; }
    public List<NavMenuItem> FooterMenuItems { get; }
    [ObservableProperty] public partial INavigator? Navigator { get; set; }

    public void Receive(NavigateToDataMessage message)
    {
        Navigator?.NavigateToData(message.Data);
    }

    public void Receive(NavigateToViewModelMessage message)
    {
        Navigator?.NavigateViewModel(message.ViewModel);
    }

    public void Receive(PaneNavigateToDataMessage message)
    {
        var map = _locator.FindByData(message.Data.GetType());
        if (map is not { View: { } viewType, ViewModel: { } vmType })
        {
            return;
        }

        var viewObj = (Control)Activator.CreateInstance(viewType)!;
        var vmObj = ActivatorUtilities.CreateInstance(Container.Services, vmType, message.Data);

        var options = new DrawerOptions
        {
            CanResize = false,
            CanLightDismiss = !message.IsInline,
            Position = Position.Right,
            MaxWidth = message.PaneWidth,
            IsCloseButtonVisible = message.IsInline,
            Buttons = DialogButton.None,
            Title = message.Title,
            MaxHeight = 700
        };

        NavigationExtensions.ConfigureView(viewObj, vmObj);

        Drawer.ShowModal(viewObj, vmObj, options: options);
    }

    public void Receive(PaneNavigateToViewModelMessage message)
    {
        var map = _locator.FindByViewModel(message.ViewModel);
        if (map is not { View: { } viewType, ViewModel: { } vmType })
        {
            return;
        }

        var viewObj = (Control)Activator.CreateInstance(viewType)!;
        var vmObj = ActivatorUtilities.CreateInstance(Container.Services, vmType);

        var options = new DrawerOptions
        {
            CanResize = false,
            CanLightDismiss = !message.IsInline,
            Position = Position.Right,
            MaxWidth = message.PaneWidth,
            IsCloseButtonVisible = message.IsInline,
            Buttons = DialogButton.None,
            Title = message.Title,
            MaxHeight = 700
        };

        NavigationExtensions.ConfigureView(viewObj, vmObj);

        Drawer.ShowModal(viewObj, vmObj, options: options);
    }

    private void UpdateSelection(NavigationResult result)
    {
        foreach (var item in MenuItems)
        {
            if (item.Tag is not NavMenuItemTag tag)
            {
                item.IsSelected = false;
                continue;
            }

            item.IsSelected = result.ViewModelType == tag.ViewModelType;
        }

        foreach (var item in FooterMenuItems)
        {
            if (item.Tag is not NavMenuItemTag tag)
            {
                item.IsSelected = false;
                continue;
            }

            item.IsSelected = result.ViewModelType == tag.ViewModelType;
        }
    }

    private static bool IsTopLevelNavItem(NavMenuItem item)
    {
        if (item.Tag is not NavMenuItemTag tag)
        {
            return false;
        }

        return tag.Parent is null;
    }

    private static bool IsFooterNavItem(NavMenuItem item)
    {
        return item.Tag is NavMenuItemTag { IsFooterItem: true };
    }

    private static void MapChildren(List<NavMenuItem> items)
    {
        List<NavMenuItem> children = [.. items.Where(x => x.Tag is NavMenuItemTag { Parent: not null })];
        foreach (var item in children)
        {
            var tag = (NavMenuItemTag)item.Tag!;
            if (items.FirstOrDefault(x => x.Header?.ToString() == tag.Parent) is { } parent)
            {
                parent.Items.Add(item);
            }
        }

    }
}