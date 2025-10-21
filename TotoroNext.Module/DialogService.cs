using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using TotoroNext.Module.Abstractions;
using Ursa.Controls;

namespace TotoroNext.Module;

public class DialogService(ILogger<DialogService> logger) : IDialogService
{
    public async Task<MessageBoxResult> Question(string title, string question)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Question asked {question}", question);
        }

        var result = await MessageBox.ShowOverlayAsync(question, title, icon: MessageBoxIcon.Question, button: MessageBoxButton.YesNo);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Answer is : {answer}", result);
        }
        
        return result;
    }
    
    public async Task Warning(string warning)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Warning : {warning}", warning);
        }
        
        await MessageBox.ShowOverlayAsync(warning, "Warning", icon: MessageBoxIcon.Warning, button: MessageBoxButton.OK);
    }

    public async Task Information(string info)
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Information : {info}", info);
        }
        
        await MessageBox.ShowOverlayAsync(info, "Info", icon: MessageBoxIcon.Information, button: MessageBoxButton.OK);
    }
    
    public async Task<MessageBoxResult> AskSkip(string type)
    {
        var messageWindow = new MessageBoxWindow(MessageBoxButton.YesNo)
        {
            Content = $"Skip {type}",
            Title = "",
            MessageIcon = MessageBoxIcon.None
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