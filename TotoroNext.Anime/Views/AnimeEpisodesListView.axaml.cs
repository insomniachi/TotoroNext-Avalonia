using Avalonia.Controls;

namespace TotoroNext.Anime.Views;

public partial class AnimeEpisodesListView : UserControl
{
    public AnimeEpisodesListView()
    {
        InitializeComponent();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox lb)
        {
            return;
        }

        if (e.AddedItems is not { Count: 1 })
        {
            return;
        }

        if (e.AddedItems[0] is not { } item)
        {
            return;
        }

        lb.ScrollIntoView(item);
    }
}