using System.Reactive.Linq;
using JetBrains.Annotations;
using ReactiveUI.Reactive;
using TotoroNext.Module;

namespace TotoroNext.Anime.Anilist.ViewModels;

[UsedImplicitly]
public class AnilistSetupViewModel(SettingsViewModel vm) : SetupWizardPageViewModel
{
    public override int Rank => 1;

    public SettingsViewModel ViewModel { get; } = vm;

    public override Task ExecuteAsync() => Task.CompletedTask;

    public override void Initialize()
    {
        ViewModel.WhenAnyValue(x => x.Token)
                 .WhereNotNull()
                 .ObserveOn(RxSchedulers.MainThreadScheduler)
                 .Subscribe(_ =>
                 {
                     CanGoNext = true;
                 });
    }
}