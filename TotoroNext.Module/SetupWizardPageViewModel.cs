using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TotoroNext.Module;

public abstract partial class SetupWizardPageViewModel : ObservableObject, ISetupWizardPageViewModel
{
    [ObservableProperty] public partial bool CanGoNext { get; set; }
    public abstract int Rank { get; }
    public abstract Task ExecuteAsync();
    public virtual void Initialize() { }
}

public interface ISetupWizardPageViewModel : INotifyPropertyChanged
{
    bool CanGoNext { get; set; }
    public int Rank { get; }
    public Task ExecuteAsync();
    void Initialize();
}