using DDS.Core.Controls;

namespace DDS.Avalonia.Controls;

public interface IReactiveWindowFor<TViewModel> : IReactiveViewFor<TViewModel>, ICoreWindowFor<TViewModel>
    where TViewModel : class
{
    
}