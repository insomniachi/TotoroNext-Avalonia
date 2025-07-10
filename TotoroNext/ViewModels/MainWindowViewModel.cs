using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INavigatorHost
{
    public MainWindowViewModel(IEnumerable<NavMenuItem> menuItems)
    {
        MenuItems = [..menuItems];
        
        WeakReferenceMessenger.Default.Register<NavigateToViewModelMessage>(this, (_, message) =>
        {
            Navigator?.NavigateViewModel(message.ViewModel);
        });
    }
    
    public List<NavMenuItem> MenuItems { get; }
    public INavigator? Navigator { get; set; }
}