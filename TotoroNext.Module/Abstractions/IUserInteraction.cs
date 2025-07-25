namespace TotoroNext.Module.Abstractions;

public interface IUserInteraction<TInput, TOutput>
{
    Task<TOutput?> GetValue(TInput input);
}

public interface ISelectionUserInteraction<T> : IUserInteraction<List<T>, T>;