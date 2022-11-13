namespace DDS.Controls;

public interface IReactiveViewFor<T> : IViewFor<T>, IProvideServices
    where T : class
{
    TopLevel GetTopLevel();

    Avalonia.Input.IInputRoot GetInputRoot();
}