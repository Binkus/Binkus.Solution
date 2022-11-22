using DDS.Core.Services;

namespace DDS.Core.ViewModels;

public interface IViewModel 
    : IRoutableViewModel, IActivatableViewModel, IProvideServices, IReactiveNotifyPropertyChanged<IReactiveObject>
{
    Guid InstanceId { get; }
    INavigationViewModel Navigation { get; }
    RoutingState Router { get; }
    string AssemblyQualifiedName { get; }
    string FullNameOfType { get; }
    string ViewModelName { get; }
    string RawViewName { get; }
    string CustomViewName { get; set; }
}

public interface IViewModelBase<T> : IViewModel
    where T : class, IViewModel
{
    // INavigationViewModel IViewModel.Navigation => Navigation;
    // new T Navigation { get; }
}

public interface IViewModelBase : IViewModel { }