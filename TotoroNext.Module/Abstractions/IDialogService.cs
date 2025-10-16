using Ursa.Controls;

namespace TotoroNext.Module.Abstractions;

public interface IDialogService
{
    Task<MessageBoxResult> Question(string title, string question);
    Task Warning(string warning);
    Task Information(string info);
    Task<MessageBoxResult> AskSkip(string type);
}