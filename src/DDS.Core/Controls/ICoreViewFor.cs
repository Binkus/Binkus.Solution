using Binkus.ReactiveMvvm;
using DDS.Core.Services;

namespace DDS.Core.Controls;

public interface ICoreViewFor<TViewModel> : ICoreView, IViewFor<TViewModel>
    where TViewModel : class
{
    
}

public interface ICoreView : IViewFor, IProvideServices
{
    bool DisposeWhenActivatedSubscription { get; set; }
    Guid Id { get; }
}