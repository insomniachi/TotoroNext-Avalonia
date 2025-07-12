using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;

namespace TotoroNext.ViewModels;

public class MainWindowViewModel : ViewModelBase, INavigatorHost
{
    public MainWindowViewModel(IEnumerable<NavMenuItem> menuItems,
                               IViewRegistry locator)
    {
        MenuItems = [..menuItems];

        WeakReferenceMessenger.Default.Register<NavigateToViewModelMessage>(this,
                                                                            (_, message) =>
                                                                            {
                                                                                Navigator?.NavigateViewModel(message
                                                                                    .ViewModel);
                                                                            });
        WeakReferenceMessenger.Default.Register<NavigateToDataMessage>(this,
                                                                       (_, message) =>
                                                                       {
                                                                           Navigator?.NavigateToData(message
                                                                               .Data);
                                                                       });

        WeakReferenceMessenger.Default.Register<PaneNavigateToDataMessage>(this, (_, message) =>
        {
            var map = locator.FindByData(message.Data.GetType());
            if (map is not { View: { } viewType, ViewModel: { } vmType })
            {
                return;
            }

            var viewObj = (Control)Activator.CreateInstance(viewType)!;
            var vmObj = ActivatorUtilities.CreateInstance(Container.Services, vmType, message.Data);

            var options = new DrawerOptions
            {
                CanResize = true,
                CanLightDismiss = !message.IsInline,
                Position = Position.Right,
                MinWidth = message.PaneWidth,
                IsCloseButtonVisible = message.IsInline,
                Buttons = DialogButton.None,
                Title = message.Title,
                MaxHeight = 700
            };

            Drawer.ShowModal(viewObj, vmObj, options: options);
        });
    }

    public List<NavMenuItem> MenuItems { get; }
    public INavigator? Navigator { get; set; }
}