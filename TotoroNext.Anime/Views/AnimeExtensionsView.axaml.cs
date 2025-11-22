using System.ComponentModel;
using Avalonia.Controls;
using TotoroNext.Anime.ViewModels;

namespace TotoroNext.Anime.Views;

public partial class AnimeExtensionsView : UserControl
{
    public AnimeExtensionsView()
    {
        InitializeComponent();
    }

    private void AutoCompleteBox_OnDropDownClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not AnimeExtensionsViewModel vm)
        {
            return;
        }
    }
}