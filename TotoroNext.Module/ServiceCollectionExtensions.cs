using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using IconPacks.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using TotoroNext.Module.Abstractions;
using TotoroNext.Module.Extensions;
using Ursa.Controls;

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
            return services.AddSingleton<IModuleSettings<TData>>(_ => new ModuleSettings<TData>(module.Descriptor));
        }

        public IServiceCollection AddMainNavigationItem<TView, TViewModel>(string header, Enum icon,
                                                                           NavMenuItemTag? tag = null)
            where TView : class, new()
            where TViewModel : class
        {
            tag ??= new NavMenuItemTag();
            tag.ViewModelType = typeof(TViewModel);

            services.AddKeyedViewMap<TView, TViewModel>(header);
            services.AddTransient(_ =>
            {
                var item = new NavMenuItem
                {
                    Header = header,
                    Icon = new Viewbox
                    {
                        Height = 20,
                        Width = 20,
                        Child = new PackIconControl
                        {
                            Kind = icon
                        }
                    },
                    Tag = tag
                };

                NavigationExtensions.SetNavigateToViewModel(item, typeof(TViewModel));

                return item;
            });
            return services;
        }

        public IServiceCollection AddChildNavigationViewItem<TView, TViewModel>(string parent, string header,
                                                                                Enum icon)
            where TView : class, new()
            where TViewModel : class
        {
            var tag = new NavMenuItemTag
            {
                Parent = parent
            };

            return services.AddMainNavigationItem<TView, TViewModel>(header, icon, tag);
        }

        public IServiceCollection AddParentNavigationViewItem(string header, Enum icon,
                                                              NavMenuItemTag? tag = null)
        {
            tag ??= new NavMenuItemTag();
            return services.AddTransient(_ =>
            {
                var item = new NavMenuItem
                {
                    Header = header,
                    Icon = new Viewbox
                    {
                        Height = 20,
                        Width = 20,
                        Child = new PackIconControl
                        {
                            Kind = icon
                        }
                    },
                    Tag = tag
                };
                return item;
            });
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

public class NavMenuItemTag
{
    public Type? ViewModelType { get; set; }
    public bool IsFooterItem { get; init; }
    public int Order { get; init; }
    public string? Parent { get; init; }
}