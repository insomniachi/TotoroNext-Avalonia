using Ursa.Controls;

namespace TotoroNext.Module.Abstractions;

public interface IDialogViewModel
{
    Task Handle(DialogResult result);
}