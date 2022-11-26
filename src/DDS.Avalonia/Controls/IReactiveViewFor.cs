using Avalonia.Input;
using DDS.Core.Controls;

namespace DDS.Avalonia.Controls;

public interface IReactiveViewFor<TViewModel> : ICoreViewFor<TViewModel>
    where TViewModel : class
{
    TopLevel GetTopLevel();

    IInputRoot GetInputRoot();
}