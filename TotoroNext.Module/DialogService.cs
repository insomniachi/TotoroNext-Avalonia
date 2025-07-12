using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Module;

public class DialogService : IDialogService
{
    public async Task<MessageBoxResult> Question(string title, string question)
    {
        return await MessageBox.ShowOverlayAsync(question, title, icon: MessageBoxIcon.Question);
    }

    public async Task<MessageBoxResult> AskSkip()
    {
        return await MessageBox.ShowAsync("Skip Section", "", MessageBoxIcon.Question, MessageBoxButton.YesNo);
    }
}