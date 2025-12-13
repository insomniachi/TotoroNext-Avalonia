using Avalonia.Controls;
using TotoroNext.Anime.ViewModels;

namespace TotoroNext.Anime.Views;

public partial class AnimeExtensionsView : UserControl
{
    public AnimeExtensionsView()
    {
        InitializeComponent();

        AutoCompleteBox.AsyncPopulator = Populate;
    }

    private async Task<IEnumerable<object>> Populate(string? term, CancellationToken ct)
    {
        if (DataContext is not AnimeExtensionsViewModel vm)
        {
            return [];
        }

        var results =  await vm.GetSearchResults(term, ct);
        return results;
    }
}