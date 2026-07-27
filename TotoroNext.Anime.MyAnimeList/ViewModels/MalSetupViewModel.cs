using JetBrains.Annotations;
using TotoroNext.Module;

namespace TotoroNext.Anime.MyAnimeList.ViewModels;

[UsedImplicitly]
public sealed class MalSetupViewModel(SettingsViewModel vm) : SetupWizardPageViewModel
{
    public override int Rank => 2;

    public SettingsViewModel ViewModel { get; } = vm;

    public override Task ExecuteAsync() => Task.CompletedTask;
}