namespace DDS.Core.Controls;

public interface ICoreViewFor<TViewModel> : IViewFor<TViewModel>, IProvideServices
    where TViewModel : class
{
    
}