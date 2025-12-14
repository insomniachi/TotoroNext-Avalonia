using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace TotoroNext.Anime.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    public static IValueConverter HtmlToTextConverter { get; } = new FuncValueConverter<string, string>(html =>
    {
        if (string.IsNullOrEmpty(html))
        {
            return "";
        }

        return html.Replace("<i>", "")
                   .Replace("</i>", "")
                   .Replace("<b>", "")
                   .Replace("</b>", "")
                   .Replace("<br><br>", Environment.NewLine);
    });
}