using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ReactiveUI;
using TotoroNext.Anime.ViewModels;

namespace TotoroNext.Anime.Views;

public partial class AnimeDetailsView : UserControl
{
    public AnimeDetailsView()
    {
        InitializeComponent();

        DataContextProperty.Changed.AddClassHandler<AnimeDetailsView>(OnDataContextChanged);
    }

    private void OnDataContextChanged(AnimeDetailsView view, AvaloniaPropertyChangedEventArgs arg)
    {
        if (arg.NewValue is not AnimeDetailsViewModel vm)
        {
            return;
        }

        vm.WhenAnyValue(x => x.Navigator)
          .WhereNotNull()
          .FirstAsync()
          .Subscribe(_ =>
          {
              if (Selector.SelectedItem is not TabStripItem item)
              {
                  return;
              }

              OnItemSelected(item, vm);
          });
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not AnimeDetailsViewModel vm)
        {
            return;
        }

        if (e.AddedItems is not { Count: 1 })
        {
            return;
        }

        var item = e.AddedItems[0] as TabStripItem;

        OnItemSelected(item, vm);
    }

    private static void OnItemSelected(TabStripItem? item, AnimeDetailsViewModel vm)
    {
        switch (item?.Content?.ToString())
        {
            case "Episodes":
                vm.Navigator?.NavigateToData(new EpisodesListViewModelNagivationParameters(vm.Anime));
                break;
            case "Related":
                vm.Navigator?.NavigateToData(vm.Anime.Related.ToList());
                break;
            case "Recommended":
                vm.Navigator?.NavigateToData(vm.Anime.Recommended.ToList());
                break;
            case "Options":
                vm.Navigator?.NavigateToData(new OverridesViewModelNavigationParameters(vm.Anime));
                break;
            case "Songs":
                vm.Navigator?.NavigateToData(new SongsViewModelNavigationParameters(vm.Anime));
                break;
        }
    }
}