using Ursa.Controls;

namespace TotoroNext.Module.Abstractions;

public interface IDialogService
{
    Task<MessageBoxResult> Question(string title, string question);
    Task Warning(string warning);
    Task<MessageBoxResult> AskSkip(string type);
}