using JetBrains.Annotations;
using TotoroNext.Module;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Torrents.RealDebrid.ViewModels;

[UsedImplicitly]
public class SettingsViewModel(IModuleSettings<Settings> data) : ModuleSettingsViewModel<Settings>(data), IInitializable;