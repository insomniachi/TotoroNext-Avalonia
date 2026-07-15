using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCoreServices()
        {
            services.AddSingleton<IViewRegistry, ViewRegistry>();
            services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            services.AddTransient<IDialogService, DialogService>();
            services.AddHostedService<InitializerService>();
            services.AddSingleton<IKeyBindingsManager, KeyBindingsManager>();
            services.AddHostedService(sp => sp.GetRequiredService<IKeyBindingsManager>());

            return services;
        }

        public IServiceCollection AddModuleSettings<TData>(IModule<TData> module)
            where TData : class, new()
        {
            return services.AddTransient<IModuleSettings<TData>>(_ => new ModuleSettings<TData>(module.Descriptor));
        }

        public IServiceCollection AddMainNavigationItem<TView, TViewModel>(string header, string iconKey,
                                                                           NavigationDrawerItemTag? tag = null)
            where TView : class, new()
            where TViewModel : class
        {
            tag ??= new NavigationDrawerItemTag();
            tag.ViewModelType = typeof(TViewModel);

            services.AddKeyedViewMap<TView, TViewModel>(header);
            services.AddTransient(_ => new NavigationDrawerItem
            {
                Header = header,
                IconKey = iconKey,
                Tag = tag
            });

            return services;
        }

        public IServiceCollection AddViewMap<TView, TViewModel>()
            where TView : class, new()
            where TViewModel : class
        {
            return services.AddViewMap(new ViewMap<TView, TViewModel>());
        }

        public IServiceCollection AddViewMap(ViewMap map)
        {
            services.AddTransient(_ => map);
            services.AddTransient(map.ViewModel);

            return services;
        }

        public IServiceCollection AddDataViewMap<TView, TViewModel, TData>()
            where TView : class, new()
            where TViewModel : class
            where TData : class
        {
            return services.AddViewMap(new DataViewMap<TView, TViewModel, TData>());
        }

        public IServiceCollection AddKeyedViewMap<TView, TViewModel>(string key)
            where TView : class, new()
            where TViewModel : class
        {
            return services.AddViewMap(new KeyedViewMap<TView, TViewModel>(key));
        }

        public IServiceCollection RegisterFactory<TService>(string key)
            where TService : notnull
        {
            services.AddTransient<IFactory<TService, Guid>, Factory<TService, Guid>>(sp =>
            {
                var factory = sp.GetRequiredService<IServiceScopeFactory>();
                var settings = sp.GetRequiredService<ILocalSettingsService>();
                return new Factory<TService, Guid>(factory, settings, key);
            });

            return services;
        }

        public IServiceCollection AddSelectionUserInteraction<TImpl, TType>()
            where TImpl : class, ISelectionUserInteraction<TType>
        {
            return services.AddTransient<ISelectionUserInteraction<TType>, TImpl>();
        }
    }
}

public class NavigationDrawerItemTag
{
    public Type? ViewModelType { get; set; }
    public bool IsFooterItem { get; init; }
    public int Order { get; init; }
}

public partial class NavigationDrawerItem : ObservableObject
{
    public required string Header { get; init; }
    public required string IconKey { get; init; }
    public NavigationDrawerItemTag? Tag { get; init; }
    [ObservableProperty] public partial string? BadgeContent { get; set; }
    [ObservableProperty] public partial bool IsSelected { get; set; }
}