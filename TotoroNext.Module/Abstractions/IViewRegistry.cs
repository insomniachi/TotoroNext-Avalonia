namespace TotoroNext.Module.Abstractions;

public interface IViewRegistry
{
    ViewMap? FindByView(Type viewType);
    ViewMap? FindByViewModel(Type vmType);
    ViewMap? FindByData(Type dataType);
    ViewMap? FindByKey(string key);
}