using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Module;

public class DialogService : IDialogService
{
    public async Task<MessageBoxResult> Question(string title, string question)
    {
        return await MessageBox.ShowOverlayAsync(question, title, icon: MessageBoxIcon.Question, button: MessageBoxButton.YesNo);
    }

    public async Task<MessageBoxResult> AskSkip()
    {
        var messageWindow = new MessageBoxWindow(MessageBoxButton.YesNo)
        {
            Content = "Skip Section",
            Title = "",
            MessageIcon = MessageBoxIcon.Question
        };

        messageWindow.Show();
        messageWindow.Topmost = true;
        var screen = messageWindow.Screens.Primary;
        if (screen is not null)
        {
            var workingArea = screen.WorkingArea; // excludes taskbar/dock
            const int padding = 20;
            messageWindow.Position = new PixelPoint(
                                                    (int)(workingArea.X + workingArea.Width - messageWindow.Width -
                                                          padding),
                                                    workingArea.Y + padding
                                                   );
        }

        var tcs = new TaskCompletionSource<MessageBoxResult>();
        messageWindow.Closed += (_, _) =>
        {
            var field = typeof(Window).GetField("_dialogResult",
                                                          BindingFlags.Instance | BindingFlags.NonPublic);
            var value = field?.GetValue(messageWindow);
            if (value is MessageBoxResult result)
            {
                tcs.TrySetResult(result);
            }
            else
            {
                tcs.TrySetResult(MessageBoxResult.Cancel);
            }
        };

        return await tcs.Task;
    }
}