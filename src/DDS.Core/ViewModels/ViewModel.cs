// ReSharper disable MemberCanBePrivate.Global

using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using Binkus.DependencyInjection;
using Binkus.ReactiveMvvm;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using DDS.Core.Helper;
using DDS.Core.Services;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Threading;

namespace DDS.Core.ViewModels;

[DataContract]
public abstract class ViewModel : ViewModel<IViewModel>
{
    protected ViewModel(IServiceProvider services, IScreen hostScreen) : base(services, hostScreen) { }
    protected ViewModel(IServiceProvider services, Lazy<IScreen> lazyHostScreen) : base(services, lazyHostScreen) { }
    protected ViewModel(IServiceProvider services) : base(services) { }
    protected ViewModel() : base(GetServicesOfCurrentScope) { }
    protected ViewModel(IScreen hostScreen) : base(TryGetServicesFromOrFromCurrentScope(hostScreen), hostScreen) { }

    private static IServiceProvider TryGetServicesFromOrFromCurrentScope(object obj) =>
        (obj as IProvideServices)?.Services ?? obj as IServiceProvider ?? GetServicesOfCurrentScope; 
    private static IServiceProvider GetServicesOfCurrentScope =>
        Ioc.Default.GetRequiredService<ServiceScopeManager>().GetCurrentScope().ServiceProvider;
}


[DataContract]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public abstract class ViewModel<TIViewModel> : ReactiveValidationObservableRecipientValidator,
    IViewModelBase,  IViewModelBase<TIViewModel>
    where TIViewModel : class, IViewModel
{
    private Lazy<IScreen>? _lazyHostScreen;
    
    /// <summary>
    /// Property to get the IScreen which contains the RoutingState / Router / Navigation
    /// <p>NOT Supported for Singleton ViewModels, use Scoped ViewModel instead.</p>
    /// </summary>
    [IgnoreDataMember, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IScreen HostScreen
    {
        get => ReturnOrWhenNullAndSingletonThrowNotSupported(_lazyHostScreen)?.Value ?? this.RaiseAndSetIfChanged(
            ref _lazyHostScreen, new Lazy<IScreen>(this.GetRequiredService<IScreen>()))!.Value;
        protected init => this.RaiseAndSetIfChanged(ref _lazyHostScreen, new Lazy<IScreen>(value));
    }

    /// <inheritdoc cref="IViewModel.Navigation"/>
    [IgnoreDataMember, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public INavigationViewModel Navigation => HostScreen as INavigationViewModel
                                              ?? this as INavigationViewModel ?? this.GetRequiredService<INavigationViewModel>();
    [IgnoreDataMember, DebuggerBrowsable(DebuggerBrowsableState.Never)] INavigationViewModel IViewModel.Navigation => Navigation;

    [IgnoreDataMember] public ViewModelActivator Activator { get; } = new();

    [IgnoreDataMember] public string ViewModelName => GetType().Name;
    [IgnoreDataMember] public virtual string CustomViewName => RawViewName;
    [IgnoreDataMember] public virtual string RawViewName => IViewModel.TryGetRawViewName(ViewModelName);
    [IgnoreDataMember] public virtual string UrlPathSegment => RawViewName.ToLowerInvariant();
    [IgnoreDataMember] string IViewModel.ViewModelName => ViewModelName;
    [IgnoreDataMember] string IViewModel.CustomViewName => CustomViewName;
    [IgnoreDataMember] string IViewModel.RawViewName => RawViewName;
    [IgnoreDataMember] string IRoutableViewModel.UrlPathSegment => UrlPathSegment;

    private volatile bool _hasKnownLifetime = true;
    private ServiceLifetime? _lifetime;

    private ServiceLifetime? Lifetime
    {
        get
        {
            // todo check potentially registered interface
            if (_lifetime is not null) return _lifetime;
            if (_hasKnownLifetime is false) return null;
            var lifetime = !Globals.IsDesignMode && ReferenceEquals(Services, Globals.Services) 
                ? ServiceLifetime.Singleton 
                : ((IKnowMyLifetime?)Services.GetService(typeof(LifetimeOf<>).MakeGenericType(GetType())))?.Lifetime;
            if (lifetime.HasValue) return _lifetime = lifetime;
            lifetime = this.GetService<IServiceCollection>()?.FirstOrDefault(x => x.ServiceType == GetType())?.Lifetime;
            if (lifetime.HasValue) return _lifetime = lifetime;
            _hasKnownLifetime = false;
            return null;
        }
    }
    
    private T ReturnOrWhenNullAndSingletonThrowNotSupported<T>(T value)
    {
        if (value is not null) return value;
        if (Lifetime is ServiceLifetime.Singleton)
        {
            throw new NotSupportedException($"Operation on Singleton not supported. {GetType().FullName}'s " +
                                            "ServiceLifetime is Singleton; consider changing lifetime to Scoped " +
                                            "or provide a HostScreen on creation.");
        }
        return value;
    }

    [DataMember] public bool RegisterAllMessagesOnActivation { get; init; } = true;
    [DataMember] public bool EnableAsyncInitPrepareActivate { get; init; } = true;

    private bool _joinInit, _joinPrepare, _joinActivation;
    [DataMember] public bool JoinInitBeforeOnActivationFinished { get => _joinInit;
        set => _joinInit = IsInitInitiated ? throw new InvalidOperationException() : value; }
    [DataMember] public bool JoinPrepareBeforeOnActivationFinished { get => _joinPrepare; 
        set => _joinInit = _joinPrepare = IsInitInitiated ? throw new InvalidOperationException() : value; }
    [DataMember] public bool JoinActivationBeforeOnActivationFinished { get => _joinActivation;
        set => _joinInit = _joinPrepare = _joinActivation = IsInitInitiated
            ? throw new InvalidOperationException() : value; }

    [IgnoreDataMember] protected JoinableTaskFactory JoinUiTaskFactory => this.GetRequiredService<JoinableTaskFactory>();
    [IgnoreDataMember] private CompositeDisposable PrepDisposables { get; set; } = new();
    [IgnoreDataMember] private CancellationTokenSource ActivationCancellationTokenSource { get; set; } = new();

    // [IgnoreDataMember] public CompositeDisposable Disposables { get; } = new();

    protected ViewModel(IServiceProvider services, IScreen hostScreen, IMessenger? messenger = null) : this(services, messenger) => _lazyHostScreen = new Lazy<IScreen>(hostScreen);
    protected ViewModel(IServiceProvider services, Lazy<IScreen> lazyHostScreen, IMessenger? messenger = null) : this(services, messenger) => _lazyHostScreen = lazyHostScreen;
    protected ViewModel(IServiceProvider services, IMessenger? messenger = null) : base(messenger, services)
    {
        Services = services;

        this.WhenActivated(disposables =>
        {
            Disposable
                .Create(OnDeactivationBase)
                .DisposeWith(disposables);
            
            Debug.WriteLine(UrlPathSegment + ":");
            
            bool isDisposed = PrepDisposables.IsDisposed;
            if (isDisposed)
            {
                ActivationCancellationTokenSource = new CancellationTokenSource();
                PrepDisposables = new CompositeDisposable();
            }
            PrepDisposables.DisposeWith(disposables);

            var token = ActivationCancellationTokenSource.Token;

            IsActive = RegisterAllMessagesOnActivation;
            OnActivation(disposables, token);

            if (EnableAsyncInitPrepareActivate)
            {
                HandleAsyncActivation(disposables, isDisposed, token);
                JoinAsyncInitPrepareActivation(disposables, token);
                return;
            } // else
            // (when async activation enabled, TrySetActivated happens on async completion after OnActivationFinishing)
            OnActivationFinishing(disposables, token);
            TrySetActivated(disposables, token);
        });
        
        Debug.WriteLine("c:"+ViewModelName);
    }

    private void HandleAsyncActivation(CompositeDisposable disposables, bool isDisposed, CancellationToken token)
    {
        if (!IsInitInitiated) return;
        
        if (isDisposed)
        {
            Prepare = JoinUiTaskFactory.RunAsync(() => OnPrepareAsync(PrepDisposables, token));
                
            if (!JoinPrepareBeforeOnActivationFinished)
            {
                // This ensures Exceptions get thrown
                RxApp.TaskpoolScheduler.Schedule(Prepare, (joinTask,_) => joinTask?.Join());
            }
        }
        // todo FirstActivation with Initialization CancellationToken (not canceled through Deactivation)
        // FirstActivation ??=
        //     JoinUiTaskFactory.RunAsync(() => OnFirstActivationBaseAsync(disposables, CancellationToken.None));
            
        Activation = JoinUiTaskFactory.RunAsync(() => OnActivationBaseAsync(disposables, token));
            
        if (!JoinActivationBeforeOnActivationFinished)
        {
            // This ensures Exceptions get thrown
            RxApp.TaskpoolScheduler.Schedule(Activation, (joinTask,_) => joinTask?.Join());
        }
    }

    [IgnoreDataMember] protected JoinableTask? Init { get; private set; }
    
    [IgnoreDataMember] protected JoinableTask? Prepare { get; private set; }

    // [IgnoreDataMember] protected JoinableTask? FirstActivation { get; private set; }
    
    [IgnoreDataMember] protected JoinableTask? Activation { get; private set; }

    [IgnoreDataMember] public bool IsInitInitiated { get; private set; }
    
    void IInitializable.Initialize(CancellationToken cancellationToken)
    {
        if (!EnableAsyncInitPrepareActivate || IsInitInitiated) return;
        
        IsInitInitiated = true;
        
        Init = JoinUiTaskFactory.RunAsync(() => InitializeAsync(cancellationToken));
        
        if (!JoinInitBeforeOnActivationFinished)
        {
            // This ensures Exceptions get thrown
            RxApp.TaskpoolScheduler.Schedule(Init, (joinTask,_) => joinTask?.Join());
        }
        
        // var handle = cancellationToken.WaitHandle;
        // todo ActivationCancellationTokenSource.Cancel() when cancellationToken gets canceled
        
        Prepare = JoinUiTaskFactory.RunAsync(() =>
            OnPrepareBaseAsync(PrepDisposables, ActivationCancellationTokenSource.Token));
        
        if (!JoinPrepareBeforeOnActivationFinished)
        {
            // This ensures Exceptions get thrown
            RxApp.TaskpoolScheduler.Schedule(Prepare, (joinTask,_) => joinTask?.Join());
        }
    }

    protected virtual Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    private async Task OnPrepareBaseAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        if (Init is { } init) await init.IgnoreExceptionAsync<Exception>();
        await OnPrepareAsync(disposables, cancellationToken).IgnoreExceptionAsync<OperationCanceledException>();
    }

    protected virtual Task OnPrepareAsync(CompositeDisposable disposables, CancellationToken cancellationToken) =>
        Task.CompletedTask;
    
    private async Task OnActivationBaseAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        if (Init is { } init) await init.IgnoreExceptionAsync<Exception>();
        if (Prepare is { } prepare) await prepare.IgnoreExceptionAsync<Exception>();
        var e = await OnActivationAsync(disposables, cancellationToken)
            .IgnoreExceptionAsync<OperationCanceledException>()
            .TryAwaitAsync<Exception>();
        OnActivationFinishing(disposables, cancellationToken);
        TrySetActivated(disposables, cancellationToken);
        e.ThrowWhenNotNull();
    }
    
    /// if !(token.IsCancellationRequested || cancelable.IsDisposed || PrepDisposables.IsDisposed) IsActivated = true;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TrySetActivated(ICancelable cancelable, CancellationToken token = default) => IsActivated = 
        !((token.IsCancellationRequested || cancelable.IsDisposed || PrepDisposables.IsDisposed) && !IsActivated);
    
    private void SetDeactivated() => IsActive = IsActivated = false;

    private bool _isActivated;
    [IgnoreDataMember] public bool IsActivated { get => _isActivated;
        private set => this.RaiseAndSetIfChanged(ref _isActivated, value); }

    protected virtual Task OnActivationAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
        => Task.CompletedTask;
    
    [SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
    [MethodImpl(MethodImplOptions.NoInlining)]
    // ReSharper disable once CognitiveComplexity
    private void JoinAsyncInitPrepareActivation(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        bool join = IsInitInitiated
                    && (JoinInitBeforeOnActivationFinished
                        || JoinPrepareBeforeOnActivationFinished
                        || JoinActivationBeforeOnActivationFinished);
        
        if (!join) return;
        
        // per default generally ignoring OperationCanceledExceptions, cause GUI App would crash unnecessarily
        CancellationToken token = default; // could be a token triggered when App closing

        var joinTask = JoinUiTaskFactory.RunAsync(async () =>
        {
            Task? init = null, prepare = null, activation = null;
            
            if (JoinInitBeforeOnActivationFinished) init = Init?.JoinAsync(token);
            if (JoinPrepareBeforeOnActivationFinished) prepare = Prepare?.JoinAsync(token);
            if (JoinActivationBeforeOnActivationFinished) activation = Activation?.JoinAsync(token);
            
            // if (JoinInitBeforeOnActivationFinished) init = Init?.Task;
            // if (JoinPrepareBeforeOnActivationFinished) prepare = Prepare?.Task;
            // if (JoinActivationBeforeOnActivationFinished) activation = Activation?.Task;
            
            List<Task> tasks = new(3);
            
            if (JoinInitBeforeOnActivationFinished && init is not null)
                tasks.Add(init.IgnoreExceptionAsync<OperationCanceledException>());
            
            if (JoinPrepareBeforeOnActivationFinished && prepare is not null)
                tasks.Add(prepare.IgnoreExceptionAsync<OperationCanceledException>());
            
            if (JoinActivationBeforeOnActivationFinished && activation is not null)
                tasks.Add(activation.IgnoreExceptionAsync<OperationCanceledException>());
            
            await tasks;
        });
        
        try
        {
            joinTask.Join(token);


            // JoinUiTaskFactory.RunAsync(async () =>
            // {
            //     for (int i = 0; i < 10; i++)
            //     {
            //         await JoinUiTaskFactory.SwitchToMainThreadAsync(true);
            //         // await RxApp.MainThreadScheduler.Yield();
            //         // await JoinUiTaskFactory.SwitchToMainThreadAsync(true);
            //         // await RxApp.MainThreadScheduler.Sleep(1.s());
            //         await Task.Delay(1000);
            //     }
            // }).JoinAsync().GetAwaiter().GetResult();
            
            
            
            // var t = JoinUiTaskFactory.RunAsync(async () =>
            // {
            //     // await joinTask;
            //     await RxApp.MainThreadScheduler.Yield();
            //     if (Init is { } init) await init;
            //     await RxApp.MainThreadScheduler.Yield();
            //     if (Prepare is { } prepare) await prepare;
            //     await RxApp.MainThreadScheduler.Yield();
            //     if (Activation is { } activation) await activation;
            //     await RxApp.MainThreadScheduler.Yield();
            // });
            
            // TaskScheduler ts = null!;
            
            // Observable.StartAsync(async () => await t, RxApp.MainThreadScheduler).SubscribeAndDisposeOnNext();
            
            // Task.Factory.StartNew(() => {}, CancellationToken.None, TaskCreationOptions.None, ts);

            // var rxs = RxApp.MainThreadScheduler;
            // rxs.Yield();

            // TaskScheduler ts = null!;
            // var t = new Task(() =>
            // {
            //     // Perform some work here...
            // }, CancellationToken.None, TaskCreationOptions.None, ts);
            
            // var mainThreadScheduler = RxApp.MainThreadScheduler;
            // var taskScheduler = mainThreadScheduler.ToTaskScheduler();
            
            // JoinUiTaskFactory

            // Task.Factory.StartNew(() => { }, TaskCreationOptions.None, TaskCreationOptions.None);

            // var joinableTask = new Task(() =>
            // {
            //     // Perform some work here...
            // }, CancellationToken.None, TaskCreationOptions.None, mainThreadScheduler.ToTaskScheduler());
        }
        catch (OperationCanceledException e)
        {
            Debug.WriteLine(e);
        }
    }
    
    protected virtual void OnActivation(CompositeDisposable disposables, CancellationToken cancellationToken){}
    protected virtual void OnActivationFinishing(CompositeDisposable disposables, CancellationToken cancellationToken){}

    // private void OnActivationFinishingBase(CompositeDisposable disposables, CancellationToken cancellationToken)
    // {
    //     // per default generally ignoring OperationCanceledExceptions, cause GUI App would crash unnecessarily
    //     CancellationToken token = default; // could be a token triggered when App closing
    //
    //     var joinTask = JoinUiTaskFactory.RunAsync(async () =>
    //     {
    //         List<Task> tasks = new(3);
    //         Init?.JoinAsync(token).IfNotNullAddTo(tasks, t => t.IgnoreExceptionAsync<OperationCanceledException>());
    //         Prepare?.JoinAsync(token).IfNotNullAddTo(tasks, t => t.IgnoreExceptionAsync<OperationCanceledException>());
    //         Activation?.JoinAsync(token).IfNotNullAddTo(tasks, t => t.IgnoreExceptionAsync<OperationCanceledException>());
    //         await tasks;
    //     });
    //     
    //     try
    //     {
    //         joinTask.Join(token);
    //     }
    //     catch (OperationCanceledException e)
    //     {
    //         Debug.WriteLine(e);
    //     }
    //     
    //     OnActivationFinishing(disposables, cancellationToken);
    // }

    private void OnDeactivationBase()
    {
        // todo join init elegantly - especially in regards to app closing, cancel init when base window closes
        ActivationCancellationTokenSource.Cancel();
        ActivationCancellationTokenSource.Dispose();
        SetDeactivated();
        OnDeactivation();
    }
    protected virtual void OnDeactivation() { }
    
    //
    
    /// For IMessenger, called automatically.
    /// <inheritdoc />
    protected sealed override void OnActivated() => base.OnActivated();
    
    /// For IMessenger, called automatically.
    /// <inheritdoc />
    protected sealed override void OnDeactivated() => base.OnDeactivated();

    //
    
    [IgnoreDataMember] public IServiceProvider Services { get; init; }
    
    //

    #region Called from View

    public virtual void OnViewActivation(CompositeDisposable disposedOnViewDeactivationDisposables) { }
    public virtual void OnViewDeactivation() { }
    public virtual void OnViewDisposal() { }

    #endregion
}
