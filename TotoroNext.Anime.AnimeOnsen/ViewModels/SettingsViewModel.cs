using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.AnimeOnsen.ViewModels;

public class SettingsViewModel(IModuleSettings<Settings> data) : ModuleSettingsViewModel<Settings>(data), IInitializable;