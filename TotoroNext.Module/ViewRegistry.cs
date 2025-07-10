using TotoroNext.Module.Abstractions;

namespace TotoroNext.Module;

public class ViewRegistry(IEnumerable<ViewMap> map) : IViewRegistry
{
    public ViewMap? FindByView(Type viewType)
    {
        return map.FirstOrDefault(x => x.View == viewType);
    }

    public ViewMap? FindByViewModel(Type vmType)
    {
        return map.FirstOrDefault(x => x.ViewModel == vmType);
    }

    public ViewMap? FindByData(Type dataType)
    {
        return map.OfType<DataViewMap>().FirstOrDefault(x => x.Data == dataType);
    }

    public ViewMap? FindByKey(string key)
    {
        return map.OfType<KeyedViewMap>().FirstOrDefault(x => x.Key == key);
    }
}


public record ViewMap(Type View, Type ViewModel);

public record ViewMap<TView, TViewModel>() : ViewMap(typeof(TView), typeof(TViewModel))
    where TView : class, new()
    where TViewModel : class;


public record KeyedViewMap(Type View, Type ViewModel, string Key) : ViewMap(View, ViewModel);

public record KeyedViewMap<TView, TViewModel>(string Key) : KeyedViewMap(typeof(TView), typeof(TViewModel), Key)
    where TView : class, new()
    where TViewModel : class;

public record DataViewMap(Type View, Type ViewModel, Type Data) : ViewMap(View, ViewModel);

public record DataViewMap<TView, TViewModel, TData>() : DataViewMap(typeof(TView), typeof(TViewModel), typeof(TData))
    where TView : class, new()
    where TViewModel : class
    where TData : class;
