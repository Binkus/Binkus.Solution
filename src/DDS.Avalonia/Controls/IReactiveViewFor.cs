using Avalonia.Input;

namespace DDS.Avalonia.Controls;

public interface IReactiveViewFor<T> : IViewFor<T>, IProvideServices
    where T : class
{
    TopLevel GetTopLevel();

    IInputRoot GetInputRoot();
}