using Avalonia.Controls;
using Avalonia.Markup.Declarative;
using Avalonia.Media;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Module;

public abstract class SelectResult<T> : ISelectionUserInteraction<T>
    where T : class
{
    public async Task<T?> GetValue(List<T> input)
    {
        var lb = new ListBox()
            .ItemsSource(input)
            .SelectionMode(SelectionMode.Single)
            .ItemTemplate<T>(CreateElement);
        
        var options = new OverlayDialogOptions()
        {
            Buttons = DialogButton.OKCancel,
            Title = GetTitle()
        };

        var result = await OverlayDialog.ShowModal(lb, null, null, options);
        
        return result == DialogResult.OK
            ? lb.SelectedItem as T
            : null;
    }
    
    protected static Image CreateImage(string? uri)
    {
        var image = new Image()
                    .Height(100)
                    .Width(75)
                    .Stretch(Stretch.UniformToFill);
        AsyncImageLoader.ImageLoader.SetSource(image, uri?.ToString());
        return image;
    }

    protected abstract Control CreateElement(T model);
    protected virtual string GetTitle() => "Select";
}
