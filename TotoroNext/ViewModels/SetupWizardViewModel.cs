using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;
using TotoroNext.Module;

namespace TotoroNext.ViewModels;

public partial class SetupWizardViewModel(IEnumerable<ISetupWizardPageViewModel> pages) : ObservableObject, IInitializable
{
    public List<ISetupWizardPageViewModel> Pages { get; } = pages.OrderBy(x => x.Rank).ToList();
    
    [ObservableProperty] public partial bool IsFirst { get; set; }
    
    [ObservableProperty] public partial bool IsLast { get; set; }

    [ObservableProperty] public partial ISetupWizardPageViewModel? CurrentPage { get; set; }

    [ObservableProperty] public partial string NextButtonText { get; set; } = "Next";

    public void Initialize()
    {
        this.WhenAnyValue(x => x.CurrentPage)
            .WhereNotNull()
            .Subscribe(x =>
            {
                IsFirst = CurrentPage == Pages.FirstOrDefault();
                IsLast = CurrentPage == Pages.LastOrDefault();
                if (IsLast)
                {
                    NextButtonText = "Finish";
                }
                x.Initialize();
            });

        CurrentPage = Pages[0];
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (CurrentPage is null)
        {
            return;
        }

        var index = Pages.IndexOf(CurrentPage);
        CurrentPage = Pages[index - 1];
    }

    [RelayCommand]
    private async Task NextPage()
    {
        if (CurrentPage is null)
        {
            return;
        }

        await CurrentPage.ExecuteAsync();
        var index = Pages.IndexOf(CurrentPage);
        CurrentPage = Pages[index + 1];
    }

    [RelayCommand]
    private void SkipPage()
    {
        if (CurrentPage is null)
        {
            return;
        }

        var index = Pages.IndexOf(CurrentPage);
        CurrentPage = Pages[index + 1];
    }
}