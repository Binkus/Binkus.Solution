// namespace DDS.ViewModels;

// class Whatever : ReactiveViewModelBase
// {
//     protected override void OnPropertyChanged(PropertyChangedEventArgs e)
//     {
//         base.OnPropertyChanged(e);
//     }
//
//     protected override void OnPropertyChanging(PropertyChangingEventArgs e)
//     {
//         base.OnPropertyChanging(e);
//     }
// }

// [ObservableObject]
// public partial class ReactiveViewModelBase : IReactiveNotifyPropertyChanged<IReactiveObject>, IHandleObservableErrors, IReactiveObject
// {
//     public ReactiveViewModelBase()
//     {
//
//     }
//
//     public IDisposable SuppressChangeNotifications()
//     {
//         throw new NotImplementedException();
//     }
//
//     public IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changing { get; }
//     public IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changed { get; }
//     public IObservable<Exception> ThrownExceptions { get; }
//     public event PropertyChangedEventHandler? PropertyChanged;
//     public event PropertyChangingEventHandler? PropertyChanging;
//     public void RaisePropertyChanging(PropertyChangingEventArgs args)
//     {
//         throw new NotImplementedException();
//     }
//
//     public void RaisePropertyChanged(PropertyChangedEventArgs args)
//     {
//         throw new NotImplementedException();
//     }
// }
