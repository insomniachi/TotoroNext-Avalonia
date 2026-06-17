using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
using TotoroNext.Module.Abstractions;
using TotoroNext.ViewModels;

namespace TotoroNext;

public class ViewLocator(IViewRegistry viewRegistry) : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        var vmType = param.GetType();
        var mapping = viewRegistry.FindByViewModel(vmType);

        if (mapping != null)
        {
            return (Control)Activator.CreateInstance(mapping.View)!;
        }
        
        return new TextBlock { Text = "Not Found: " + vmType.Name };
    }

    public bool Match(object? data)
    {
        return data is ObservableObject;
    }
}