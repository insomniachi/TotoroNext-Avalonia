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
    private bool _isProgarmaticNavigation;

    public MainWindowViewModel(IEnumerable<NavigationDrawerItem> menuItems,
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
        var items = menuItems.OrderBy(x => x.Tag is not { } tag ? 0 : tag.Order).ToList();
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

    public List<NavigationDrawerItem> MenuItems { get; }
    public List<NavigationDrawerItem> FooterMenuItems { get; }

    [ObservableProperty] public partial NavigationDrawerItem? SelectedMenuItem { get; set; }

    [ObservableProperty] public partial NavigationDrawerItem? SelectedFooterMenuItem { get; set; }

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

        var viewObj = (Page)Activator.CreateInstance(viewType)!;
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

        OverlayDrawer.ShowStandard(viewObj, vmObj, options: options);
    }

    public void Receive(PaneNavigateToViewModelMessage message)
    {
        var map = _locator.FindByViewModel(message.ViewModel);
        if (map is not { View: { } viewType, ViewModel: { } vmType })
        {
            return;
        }

        var viewObj = (Page)Activator.CreateInstance(viewType)!;
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

        OverlayDrawer.ShowStandard(viewObj, vmObj, options: options);
    }

    partial void OnSelectedMenuItemChanged(NavigationDrawerItem? value)
    {
        if (value is not { Tag.ViewModelType: not null })
        {
            return;
        }

        // Clear footer selection
        SelectedFooterMenuItem = null;

        // Update IsSelected states
        foreach (var item in MenuItems)
        {
            item.IsSelected = item == value;
        }

        foreach (var item in FooterMenuItems)
        {
            item.IsSelected = false;
        }

        if (!_isProgarmaticNavigation)
        {
            Navigator?.NavigateViewModel(value.Tag.ViewModelType);
        }
    }

    partial void OnSelectedFooterMenuItemChanged(NavigationDrawerItem? value)
    {
        if (value is not { Tag.ViewModelType: not null })
        {
            return;
        }

        // Clear menu selection
        SelectedMenuItem = null;

        // Update IsSelected states
        foreach (var item in FooterMenuItems)
        {
            item.IsSelected = item == value;
        }

        foreach (var item in MenuItems)
        {
            item.IsSelected = false;
        }

        if (!_isProgarmaticNavigation)
        {
            Navigator?.NavigateViewModel(value.Tag.ViewModelType);
        }
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
        _isProgarmaticNavigation = true;

        foreach (var item in MenuItems)
        {
            if (item.Tag is not { } tag)
            {
                item.IsSelected = false;
                continue;
            }

            var isMatch = result.ViewModelType == tag.ViewModelType;
            item.IsSelected = isMatch;

            if (!isMatch)
            {
                continue;
            }

            SelectedMenuItem = item;
            _isProgarmaticNavigation = false;
            return;
        }

        foreach (var item in FooterMenuItems)
        {
            if (item.Tag is not { } tag)
            {
                item.IsSelected = false;
                continue;
            }

            var isMatch = result.ViewModelType == tag.ViewModelType;
            item.IsSelected = isMatch;

            if (!isMatch)
            {
                continue;
            }

            SelectedFooterMenuItem = item;
            _isProgarmaticNavigation = false;
            return;
        }
    }

    private static bool IsTopLevelNavItem(NavigationDrawerItem item)
    {
        if (item.Tag is not { } tag)
        {
            return false;
        }

        return tag.Parent is null;
    }

    private static bool IsFooterNavItem(NavigationDrawerItem item)
    {
        return item.Tag is { IsFooterItem: true };
    }

    private static void NavigateToDialog(ViewMap map, NavigateToDialogMessage message)
    {
        var viewObj = (Page)Activator.CreateInstance(map.View)!;
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

        Dialog.ShowStandardAsync(viewObj, vmObj, options: options)
              .ContinueWith(x =>
              {
                  if (vmObj is IDialogViewModel vm)
                  {
                      vm.Handle(x.Result);
                  }
              });
    }
}