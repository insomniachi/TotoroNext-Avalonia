using System.Diagnostics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
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

    public async Task<MessageBoxResult> AskSkip(string type, MessageBoxResult defaultResult = MessageBoxResult.No)
    {
        var progressBar = new ProgressBar
        {
            Minimum = 0,
            Maximum = 5,
            Value = 0,
            Height = 6,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var contentPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Children =
            {
                new TextBlock { Text = $"Skip {type}", HorizontalAlignment = HorizontalAlignment.Center },
                progressBar
            }
        };

        var messageWindow = new MessageBoxWindow(MessageBoxButton.YesNo)
        {
            Content = contentPanel,
            Title = "",
            MessageIcon = MessageBoxIcon.None,
            ShowInTaskbar = false
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

        var sw = Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        timer.Tick += (_, _) =>
        {
            var elapsed = sw.Elapsed.TotalSeconds;
            progressBar.Value = Math.Min(5, elapsed);

            if (!(elapsed >= 5))
            {
                return;
            }

            try
            {
                timer.Stop();
            }
            catch (ObjectDisposedException) { }

            var field = typeof(Window).GetField("_dialogResult", BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(messageWindow, defaultResult);
            messageWindow.Close();
        };

        timer.Start();

        return await tcs.Task;
    }
}