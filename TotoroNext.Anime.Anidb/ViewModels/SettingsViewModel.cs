using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Anime.Anidb.ViewModels;

public class SettingsViewModel(IModuleSettings<Settings> data) : ModuleSettingsViewModel<Settings>(data), IInitializable; 
