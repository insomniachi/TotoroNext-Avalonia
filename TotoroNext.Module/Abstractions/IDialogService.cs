using Ursa.Controls;

namespace TotoroNext.Module.Abstractions;

public interface IDialogService
{
    Task<MessageBoxResult> Question(string title, string question);
    Task Warning(string warning);
    Task Information(string info);
    Task<bool> EditDataContainer(string title, List<DataContainerProperty> options);
    Task<bool> EditModuleOptions(Guid id, string componentType);
    Task<MessageBoxResult> AskSkip(string type, MessageBoxResult defaultResult = MessageBoxResult.No);
}