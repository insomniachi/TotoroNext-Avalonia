using Avalonia.Controls;
using Avalonia.Xaml.Interactivity;
using TotoroNext.Anime.Abstractions.Behaviors;
using TotoroNext.Anime.Abstractions.Controls;

namespace TotoroNext.Anime.Views;

public partial class UserListView : UserControl
{
    public UserListView()
    {
        InitializeComponent();
    }

    private void ItemsGrid_OnElementPrepared(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is not AnimeCard card)
        {
            return;
        }

        var behaviors = Interaction.GetBehaviors(card);
        foreach (var behavior in behaviors)
        {
            if (behavior is IVirtualizingBehavior<AnimeCard> virtualizingBehavior)
            {
                virtualizingBehavior.Update(card);
            }
        }
    }
}