using JetBrains.Annotations;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AllAnime.ViewModels;

[UsedImplicitly]
public class SettingsViewModel(IModuleSettings<Settings> settings) : ModuleSettingsViewModel<Settings>(settings), IInitializable;