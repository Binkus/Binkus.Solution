using DDS.Core.Services;

namespace DDS.Core.Controls;

public interface ICoreViewFor<TViewModel> : ICoreView, IViewFor<TViewModel>, IProvideServices
    where TViewModel : class
{
}

public interface ICoreView
{
    bool DisposeWhenActivatedSubscription { get; set; }
    Guid Id { get; }
}