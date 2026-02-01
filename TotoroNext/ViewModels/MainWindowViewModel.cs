using System.Reactive.Linq;
using System.Text.Json;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using TotoroNext.Module.Extensions;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;
using Velopack;

namespace TotoroNext.ViewModels;

public partial class MainWindowViewModel : ObservableObject,
                                           INavigatorHost,
                                           IRecipient<NavigateToViewModelMessage>,
                                           IRecipient<NavigateToDataMessage>,
                                           IRecipient<PaneNavigateToViewModelMessage>,
                                           IRecipient<PaneNavigateToDataMessage>,
                                           IRecipient<NavigateToDialogMessage>,
                                           IRecipient<NavigateToViewModelDialogMessage>,
                                           IRecipient<NavigateToKeyDialogMessage>
{
    private readonly IDialogService _dialogService;
    private readonly IViewRegistry _locator;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IMessenger _messenger;
    private readonly UpdateManager _updateManager;

    public MainWindowViewModel(IEnumerable<NavMenuItem> menuItems,
                               IViewRegistry locator,
                               IMessenger messenger,
                               IDialogService dialogService,
                               ILogger<MainWindowViewModel> logger,
                               SettingsModel settings,
                               UpdateManager updateManager)
    {
        _locator = locator;
        _messenger = messenger;
        _dialogService = dialogService;
        _logger = logger;
        _updateManager = updateManager;
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
                navigator.NavigateToRoute(settings.HomeView ?? "Home");
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
    
    public void Receive(NavigateToDialogMessage message)
    {
        if (message.Data is null)
        {
            return;
        }
        
        var map = _locator.FindByData(message.Data.GetType());

        if (map is null)
        {
            return;
        }

        NavigateToDialog(map, message);
    }

    public void Receive(NavigateToKeyDialogMessage message)
    {
        var map = _locator.FindByKey(message.Key);

        if (map is null)
        {
            return;
        }

        NavigateToDialog(map, message);
    }


    public void Receive(NavigateToViewModelDialogMessage message)
    {
        var map = _locator.FindByViewModel(message.ViewModel);

        if (map is null)
        {
            return;
        }

        NavigateToDialog(map, message);
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
            MinWidth = message.PaneWidth,
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
            MinWidth = message.PaneWidth,
            IsCloseButtonVisible = message.IsInline,
            Buttons = DialogButton.None,
            Title = message.Title,
            MaxHeight = 700
        };

        NavigationExtensions.ConfigureView(viewObj, vmObj);

        Drawer.ShowModal(viewObj, vmObj, options: options);
    }

    [UsedImplicitly]
    public async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo is null)
            {
                return;
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Update found: {info}", JsonSerializer.Serialize(updateInfo));
            }

            var answer = await _dialogService.Question("Update found", $"Download and install {updateInfo.TargetFullRelease.Version}?");

            if (answer == MessageBoxResult.Yes)
            {
                _messenger.Send(new NavigateToViewModelDialogMessage
                {
                    Button = DialogButton.None,
                    CloseButtonVisible = false,
                    Title = "Downloading Update",
                    ViewModel = typeof(DownloadUpdateViewModel),
                    Data = updateInfo
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
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

    private static void NavigateToDialog(ViewMap map, NavigateToDialogMessage message)
    {
        var viewObj = (Control)Activator.CreateInstance(map.View)!;
        var vmObj = message.Data is null
            ? ActivatorUtilities.CreateInstance(Container.Services, map.ViewModel)
            : ActivatorUtilities.CreateInstance(Container.Services, map.ViewModel, message.Data);

        var options = new DialogOptions
        {
            CanResize = false,
            IsCloseButtonVisible = message.CloseButtonVisible,
            Title = message.Title,
            Button = message.Button,
            ShowInTaskBar = false
        };

        NavigationExtensions.ConfigureView(viewObj, vmObj);

        Dialog.ShowModal(viewObj, vmObj, options: options)
              .ContinueWith(x =>
              {
                  if (vmObj is IDialogViewModel vm)
                  {
                      vm.Handle(x.Result);
                  }
              });
    }
}