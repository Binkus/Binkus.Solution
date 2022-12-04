// ReSharper disable MemberCanBePrivate.Global

using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using DDS.Core.Helper;
using DDS.Core.Services;
using DynamicData.Binding;
using Microsoft.VisualStudio.Threading;

namespace DDS.Core.ViewModels;

[DataContract]
public abstract class ViewModelBase : ViewModelBase<IViewModel>
{
    protected ViewModelBase(IServiceProvider services, IScreen hostScreen) : base(services, hostScreen) { }
    protected ViewModelBase(IServiceProvider services, Lazy<IScreen> lazyHostScreen) : base(services, lazyHostScreen){}
    protected ViewModelBase(IServiceProvider services) : base(services) { }
}


[DataContract]
[SuppressMessage("ReSharper", "StringLiteralTypo")]
public abstract class ViewModelBase<TIViewModel> : ReactiveObservableObject,
    IViewModelBase,  IViewModelBase<TIViewModel>, IEquatable<ViewModelBase<TIViewModel>>
    where TIViewModel : class, IViewModel
{
    [DataMember] public string UrlPathSegment { get; }

    private Lazy<IScreen>? _lazyHostScreen;
    
    /// <summary>
    /// Property to get the IScreen which contains the RoutingState / Router / Navigation
    /// <p>NOT Supported for Singleton ViewModels, use Scoped ViewModel instead.</p>
    /// </summary>
    [IgnoreDataMember, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public virtual IScreen HostScreen
    {
        get => ReturnOrWhenNullAndSingletonThrowNotSupported(_lazyHostScreen)?.Value ?? this.RaiseAndSetIfChanged(
            ref _lazyHostScreen, new Lazy<IScreen>(GetService<IScreen>()))!.Value;
        protected init => this.RaiseAndSetIfChanged(ref _lazyHostScreen, new Lazy<IScreen>(value));
    }

    /// <summary>
    /// Used for Navigation / Routing
    /// <p>NOT Supported for Singleton ViewModels, use Scoped ViewModel
    /// if you want to use the Navigation from this ViewModel.</p>
    /// </summary>
    [IgnoreDataMember, DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public INavigationViewModel Navigation => HostScreen as INavigationViewModel
                                              ?? this as INavigationViewModel ?? GetService<INavigationViewModel>();

    [IgnoreDataMember] public ViewModelActivator Activator { get; } = new();

    [DataMember] public Guid InstanceId { get; } = Guid.NewGuid();
    [DataMember] public string ViewModelName { get; }
    [DataMember] public string RawViewName { get; }
    
    [DataMember]
    public string CustomViewName
    {
        // removes "ViewModel" or e.g. "ViewModel'1" at the end if possible, "MainViewModel" => "Main"
        get => _customViewName ??= ViewModelName.EndsWith("ViewModel") ? ViewModelName[..^9]
            : ViewModelName[^11..^2] == "ViewModel" ? ViewModelName[..^11] : ViewModelName;
        set => this.RaiseAndSetIfChanged(ref _customViewName, value);
    }
    private string? _customViewName;

    private bool _hasKnownLifetime = true;
    private ServiceLifetime? _lifetime;

    private ServiceLifetime? Lifetime
    {
        get
        {
            if (_lifetime is not null) return _lifetime;
            if (_hasKnownLifetime is false) return null;
            var lifetime = !Globals.IsDesignMode && ReferenceEquals(Services, Globals.Services) 
                ? ServiceLifetime.Singleton 
                : ((IKnowMyLifetime?)Services.GetService(typeof(LifetimeOf<>).MakeGenericType(GetType())))?.Lifetime;
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
                                            "ServiceLifetime is Singleton; consider changing lifetime to Scoped.");
        }
        return value;
    }

    private bool _joinInit, _joinPrepare, _joinActivation; 
    [DataMember] public bool JoinInitBeforeOnActivationFinished { get => _joinInit;
        set => _joinInit = IsInitInitiated ? throw new InvalidOperationException() : value; }
    [DataMember] public bool JoinPrepareBeforeOnActivationFinished { get => _joinPrepare; 
        set => _joinInit = _joinPrepare = IsInitInitiated ? throw new InvalidOperationException() : value; }
    [DataMember] public bool JoinActivationBeforeOnActivationFinished { get => _joinActivation;
        set => _joinInit = _joinPrepare = _joinActivation = IsInitInitiated
            ? throw new InvalidOperationException() : value; }

    [IgnoreDataMember]
    private JoinableTaskFactory JoinUiTaskFactory { get; } =
        new(new JoinableTaskContext(Thread.CurrentThread, SynchronizationContext.Current));
    [IgnoreDataMember] private CompositeDisposable PrepDisposables { get; set; } = new();
    [IgnoreDataMember] private CancellationTokenSource ActivationCancellationTokenSource { get; set; } = new();

    // [IgnoreDataMember] public CompositeDisposable Disposables { get; } = new();

    protected ViewModelBase(IServiceProvider services, IScreen hostScreen) : this(services) => _lazyHostScreen = new Lazy<IScreen>(hostScreen);
    protected ViewModelBase(IServiceProvider services, Lazy<IScreen> lazyHostScreen) : this(services) => _lazyHostScreen = lazyHostScreen;
    protected ViewModelBase(IServiceProvider services)
    {
        Services = services;
        var type = GetType().UnderlyingSystemType;
        ViewModelName = type.Name;
        RawViewName = CustomViewName;
        UrlPathSegment = $"/{RawViewName.ToLowerInvariant()}?id={InstanceId}";

        // this.WhenPropertyChanged(x => x.Init)
        //     .Select(x => x).Subscribe(_ =>
        //         Prepare = JoinUiTaskFactory.RunAsync(() => PrepareBaseAsync(PrepDisposables, default)))
        //     .DisposeWith(PrepDisposables);

        this.WhenActivated(disposables =>
        {
            Debug.WriteLine(UrlPathSegment + ":");
            
            bool isDisposed = PrepDisposables.IsDisposed;
            if (isDisposed)
                ActivationCancellationTokenSource = new CancellationTokenSource();
            
            var token = ActivationCancellationTokenSource.Token;
            
            if (isDisposed)
            {
                PrepDisposables = new CompositeDisposable();
                Prepare = JoinUiTaskFactory.RunAsync(() => OnPrepareAsync(PrepDisposables, token));
                
                if (!JoinPrepareBeforeOnActivationFinished)
                {
                    // This ensures Exceptions get thrown
                    RxApp.TaskpoolScheduler.Schedule(Prepare, (joinTask,_) => joinTask?.Join());
                }
            }
            PrepDisposables.DisposeWith(disposables);
            
            // RxApp.MainThreadScheduler.ScheduleAsync(Prepare, (_, joinTask, _) => joinTask.Task);
            // RxApp.MainThreadScheduler.ScheduleAsync(Prepare, (_, joinTask, _) => joinTask.JoinAsync());

            // var taskCompletionSource = new TaskCompletionSource(Init, TaskCreationOptions.AttachedToParent);
            // var taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            // Activation = taskCompletionSource.Task;

            // todo FirstActivation with Initialization CancellationToken (not canceled through Deactivation)
            // FirstActivation ??=
            //     JoinUiTaskFactory.RunAsync(() => OnFirstActivationBaseAsync(disposables, CancellationToken.None));
            
            Activation = JoinUiTaskFactory.RunAsync(() => OnActivationBaseAsync(disposables, token));
            
            if (!JoinActivationBeforeOnActivationFinished)
            {
                // This ensures Exceptions get thrown
                RxApp.TaskpoolScheduler.Schedule(Activation, (joinTask,_) => joinTask?.Join());
            }

            // RxApp.MainThreadScheduler.ScheduleAsync((CancellationToken token) => 
            //     HandleActivationBaseAsync(disposables, taskCompletionSource, token)
            //         .ContinueWith(a => 
            //             a.TrySetResultsToSource(taskCompletionSource, token), TaskScheduler.Current),
            //     static (_, activateAsync, token) => activateAsync.Invoke(token));
            
            //

            // Task? t = null;
            // CancellationToken? ct = null;
            // Observable.StartAsync((CancellationToken token) =>
            //         {
            //             ct = token;
            //             return t = HandleActivationBaseAsync(disposables, taskCompletionSource, token);
            //         },
            //         RxApp.MainThreadScheduler)
            //     .ObserveOn(RxApp.TaskpoolScheduler)
            //     .SubscribeOn(RxApp.TaskpoolScheduler)
            //     .Subscribe(_ => t?.TrySetResultsToSource(taskCompletionSource, ct))
            //     ;
            
            //

            // this.WhenAnyValue(x => x.Init).Select(init => init)
            //     .ObserveOn(RxApp.TaskpoolScheduler)
            //     .Subscribe(init =>
            //     {
            //         Console.WriteLine("onNext");
            //         if (init is not null)
            //         {
            //             RxApp.MainThreadScheduler.ScheduleAsync(disposables,
            //                     (_, d, token) =>
            //                         HandleActivationBase(d, taskCompletionSource, token)
            //                             .ContinueWith(a => 
            //                                 a.TrySetResultsToSource(taskCompletionSource, token, false)))
            //                 // init.ContinueWith(_ =>
            //                 //     HandleActivation(d, token).ContinueWith(_ =>
            //                 // _.TrySetResultsToSource(taskCompletionSource, token))))
            //                 .DisposeWith(disposables);
            //         }
            //     }).DisposeWith(disposables);

            // RxApp.MainThreadScheduler.ScheduleAsync(
            //         (CancellationToken t) => CreateHandleActivationTask(disposables, t),
            //         static (_, state, token) => state.Invoke(token))
            //     .DisposeWith(disposables);
            
            // RxApp.MainThreadScheduler.Schedule(() => HandleActivation(disposables), (_, state)
            //         => Observable.StartAsync(state, RxApp.MainThreadScheduler).Subscribe())
            //     .DisposeWith(disposables);

            JoinAsyncInitPrepareActivation(disposables, token);
            Disposable
                .Create(OnDeactivationBase)
                .DisposeWith(disposables);
        });
        
        Debug.WriteLine("c:"+ViewModelName);
    }

    // JoinableTask IInitializable.Init { set => Init = value; }

    // private TaskCompletionSource _taskCompletionSource = null!;

    // private Task? _init;
    // public Task Init { get => _init ?? Task.CompletedTask; private set => _init = value; }
    // private JoinableTask? _init;
    // public JoinableTask? Init { get => _init; private set => this.RaiseAndSetIfChanged(ref _init, value); }
    [IgnoreDataMember] protected JoinableTask? Init { get; private set; }
    
    [IgnoreDataMember] protected JoinableTask? Prepare { get; private set; }

    // [IgnoreDataMember] protected JoinableTask? FirstActivation { get; private set; }
    
    // private Task? _activation;
    // public Task Activation { get => _activation ?? Task.CompletedTask; private set => _activation = value; }
    [IgnoreDataMember] protected JoinableTask? Activation { get; private set; }

    [IgnoreDataMember] public bool IsInitInitiated { get; private set; }
    
    void IInitializable.Initialize(CancellationToken cancellationToken)
    {
        IsInitInitiated = true;
        // Init = JoinUiTaskFactory.RunAsync(() => Observable.StartAsync(() => 
        //     InitializeAsync(cancellationToken), RxApp.MainThreadScheduler).ToTask());
        
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
        // await JoinUiTaskFactory.SwitchToMainThreadAsync(true);
        if (Init is { } init) await init.IgnoreExceptionAsync<OperationCanceledException>();
        await OnPrepareAsync(disposables, cancellationToken).IgnoreExceptionAsync<OperationCanceledException>();
    }

    protected virtual Task OnPrepareAsync(CompositeDisposable disposables, CancellationToken cancellationToken) =>
        Task.CompletedTask;
    
    private async Task OnActivationBaseAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        // await JoinUiTaskFactory.SwitchToMainThreadAsync(true);
        
        // per default generally ignoring OperationCanceledExceptions, cause GUI App would crash unnecessarily
        // explicitly in this method additionally cause HandleActivationAsync would not be executed at all when
        // Prepare is throwing OperationCanceledException, which would violate the implemented idea of
        // Creation => Init => Prepare; VM-Active => Prep (skip first prepare done by Init) => HandleActivationAsync
        // HandleActivation awaits Async variant which awaits Preparation which awaits Init, executing one after another
        if (Init is { } init) await init.IgnoreExceptionAsync<OperationCanceledException>();
        if (Prepare is { } prepare) await prepare.IgnoreExceptionAsync<OperationCanceledException>();
        await OnActivationAsync(disposables, cancellationToken).IgnoreExceptionAsync<OperationCanceledException>();
        // source.TrySetResult();
        
        // return Observable.StartAsync(() => HandleActivationBase(disposables, cancellationToken),
        // RxApp.MainThreadScheduler).ToTask(cancellationToken);

        // var d = disposables;
        // var t = cancellationToken;
        //
        // if (Init is null) throw new NullReferenceException();
        //
        // return Init.ContinueWith(init =>
        //         init.TrySetResultsToSource(source, t, false, 
        //             () => HandleActivation(d, t)))
        //     .ContinueWith(activation => activation.TrySetResultsToSource(source, t));
        // .ContinueWith(async _ =>
        // {
        //     await _;
        //     await SetResults(_, source, token);
        //     await Init;
        //     await Activation;
        //     source.SetResult();
        // });

        TrySetActivated(disposables, cancellationToken);
    }
    
    private readonly object _activationMutex = new();
    private void TrySetActivated(ICancelable cancelable, CancellationToken token = default)
    {
        if (token.IsCancellationRequested || cancelable.IsDisposed || PrepDisposables.IsDisposed) return;
        lock (_activationMutex)
        {
            if (token.IsCancellationRequested || cancelable.IsDisposed || PrepDisposables.IsDisposed) return;
            IsActivated = true;
        }
    }
    private void SetDeactivated() => IsActivated = false;

    private bool _isActivated;
    [IgnoreDataMember] public bool IsActivated { get => _isActivated;
        private set => this.RaiseAndSetIfChanged(ref _isActivated, value); }

    protected virtual Task OnActivationAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
        => Task.CompletedTask;
    
    [SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
    // ReSharper disable once CognitiveComplexity
    private void JoinAsyncInitPrepareActivation(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        // this.WhenAnyValue(x => x.Init).Select(x => x)
        //     .ObserveOn(RxApp.TaskpoolScheduler)
        //     .Synchronize()
        //     .SubscribeOn(RxApp.MainThreadScheduler)
        //     .Subscribe(x => x?.GetAwaiter().GetResult()).Dispose();
        //
        // this.WhenAnyValue(x => x.Activation).Select(x => x)
        //     .ObserveOn(RxApp.TaskpoolScheduler)
        //     .Synchronize()
        //     .SubscribeOn(RxApp.MainThreadScheduler)
        //     .Subscribe(x => x?.GetAwaiter().GetResult()).Dispose();
        //
        //
        // RxApp.MainThreadScheduler.Sleep(15.Seconds()).GetAwaiter().GetResult();
        
        // Init?.ConfigureAwait(false).GetAwaiter().GetResult();
        // Activation?.ConfigureAwait(false).GetAwaiter().GetResult();

        // Task.Run(async () => await Activation!.ConfigureAwait(false))
        //     .ConfigureAwait(false).GetAwaiter().GetResult();
        // Activation?.GetAwaiter().GetResult();
        // Activation?.AwaitBeforeExecution(Init ?? Task.CompletedTask).GetAwaiter().GetResult();
        
        //

        // var jtf = new JoinableTaskFactory(new JoinableTaskContext(Thread.CurrentThread,
        //     SynchronizationContext.Current));
        // var init = jtf.RunAsync(async () => { if (Init is not null) await Init; });
        // var activation = jtf.RunAsync(async () => { if (Activation is not null) await Activation; });
        //
        // var joinTask = jtf.RunAsync(async () =>
        // {
        //     var t0 = init.JoinAsync();
        //     var t1 = activation.JoinAsync();
        //     await t0;
        //     await t1;
        // });
        // joinTask.Join();
        
        //
        
        // per default not canceling joinTask, cause it would partially mess up the execution order, and
        // per default generally ignoring OperationCanceledExceptions, cause GUI App would crash unnecessarily
        CancellationToken token = default; // could be a token triggered when App closing
        
        bool join = IsInitInitiated
                    && (JoinInitBeforeOnActivationFinished 
                    || JoinPrepareBeforeOnActivationFinished 
                    || JoinActivationBeforeOnActivationFinished);

        JoinableTask? joinTask = !join ? null : JoinUiTaskFactory.RunAsync(async () =>
        {
            // await JoinUiTaskFactory.SwitchToMainThreadAsync();

            Task? init = null, prepare = null, activation = null;
            
            if (JoinInitBeforeOnActivationFinished) init = Init?.JoinAsync(token);
            if (JoinPrepareBeforeOnActivationFinished) prepare = Prepare?.JoinAsync(token);
            if (JoinActivationBeforeOnActivationFinished) activation = Activation?.JoinAsync(token);
            
            //
            
            // if (JoinInitBeforeOnActivationFinished && init is not null)
            //     await init.IgnoreExceptionAsync<OperationCanceledException>();
            //
            // if (JoinPrepareBeforeOnActivationFinished && prepare is not null)
            //     await prepare.IgnoreExceptionAsync<OperationCanceledException>();
            //
            // if (JoinActivationBeforeOnActivationFinished && activation is not null)
            //     await activation.IgnoreExceptionAsync<OperationCanceledException>();
            
            //
            
            List<Task> tasks = new(3);
            
            if (JoinInitBeforeOnActivationFinished && init is not null)
                tasks.Add(init.IgnoreExceptionAsync<OperationCanceledException>());
            
            if (JoinPrepareBeforeOnActivationFinished && prepare is not null)
                tasks.Add(prepare.IgnoreExceptionAsync<OperationCanceledException>());
            
            if (JoinActivationBeforeOnActivationFinished && activation is not null)
                tasks.Add(activation.IgnoreExceptionAsync<OperationCanceledException>());

            await tasks;
        });

        if (join)
            try
            {
                // currently any join would block all tasks scheduled to MainThread
                // todo make sync join somehow async while sync by doing continuations of Tasks scheduled to MainThread
                // probably not possible
                
                // joinTask.Join(token);
                // var t = joinTask.JoinAsync(token);
                // var d = RxApp.MainThreadScheduler.ScheduleAsync(t, (scheduler, task, arg3) => task);
                // JoinUiTaskFactory.RunAsync(() => t).Join();

                joinTask?.Join(token);
            }
            catch (OperationCanceledException e)
            {
                Debug.WriteLine(e);
            }
        
        // todo notification system - IObservable<bool> for canExecute for parent Views to temporarily optionally
        // todo ^| disable e.g. Buttons while Initialization after WhenActivated called

        OnActivationFinished(disposables, cancellationToken);
    }
    protected virtual void OnActivationFinished(CompositeDisposable disposables, CancellationToken cancellationToken){}

    private void OnDeactivationBase()
    {
        // todo schedule after Activation task & locking mechanism | atomic
        SetDeactivated();
        
        ActivationCancellationTokenSource.Cancel();
        ActivationCancellationTokenSource.Dispose();
        // todo put recreation of ActivationCancellationTokenSource in here
        OnDeactivation();
    }
    protected virtual void OnDeactivation() { }
    
    [IgnoreDataMember] public IServiceProvider Services { get; protected init; }

    public TService GetService<TService>() where TService : notnull => Services.GetRequiredService<TService>();
    
    public object GetService(Type serviceType) => Services.GetRequiredService(serviceType);


    #region Simplified Command Creation

    public ReactiveCommand<Unit, IRoutableViewModel> NavigateReactiveCommand<TViewModel>(IObservable<bool>? canExecute = default)
        where TViewModel : class, IRoutableViewModel 
        => CreateNavigationReactiveCommandFromObservable<TViewModel>(
            new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => Navigation.Router.Navigate), canExecute);
    
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateAndResetReactiveCommand<TViewModel>(IObservable<bool>? canExecute = default)
        where TViewModel : class, IRoutableViewModel 
        => CreateNavigationReactiveCommandFromObservable<TViewModel>(
            new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => Navigation.Router.NavigateAndReset), canExecute);

    public ReactiveCommand<Unit, IRoutableViewModel> CreateNavigationReactiveCommandFromObservable<TViewModel>(
        Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>> navi, IObservable<bool>? canExecute = default) where TViewModel : class, IRoutableViewModel
        => ReactiveCommand.CreateFromObservable(
            () => navi.Value.Execute(GetService<TViewModel>()),
            // todo make more lazy, can execute can load values, when returned cmd is e.g. used as bound cmd to view
            canExecute: canExecute ?? this.WhenAnyObservable(x => x.Navigation.Router.CurrentViewModel).Select(x => x is not TViewModel)
        );
    
    //

    public ReactiveCommand<Unit, IRoutableViewModel> NavigateReactiveCommand(Type viewModelType, IObservable<bool>? canExecute = default)
        => CreateNavigationReactiveCommandFromObservable(viewModelType, 
            new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => Navigation.Router.Navigate), canExecute);
    
    public ReactiveCommand<Unit, IRoutableViewModel> NavigateAndResetReactiveCommand(Type viewModelType, IObservable<bool>? canExecute = default)
        => CreateNavigationReactiveCommandFromObservable(viewModelType, 
            new Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>>(() => Navigation.Router.NavigateAndReset),canExecute);
    
    public ReactiveCommand<Unit, IRoutableViewModel> CreateNavigationReactiveCommandFromObservable(Type viewModelType,
        Lazy<ReactiveCommandBase<IRoutableViewModel, IRoutableViewModel>> navi, IObservable<bool>? canExecute = default)
    {
        if (viewModelType.IsAssignableTo(typeof(IRoutableViewModel)) is false) throw new InvalidOperationException();
        return ReactiveCommand.CreateFromObservable(
            () => navi.Value.Execute((IRoutableViewModel)GetService(viewModelType)),
            // todo make more lazy, can execute can load values, when returned cmd is e.g. used as bound cmd to view
            canExecute: canExecute ?? this.WhenAnyObservable(x => x.Navigation.Router.CurrentViewModel)
                .Select(x => !x?.GetType().IsAssignableTo(viewModelType) ?? true)
        );
    }
    
    #endregion

    #region Called from View

    public virtual void OnViewActivation(CompositeDisposable disposedOnViewDeactivationDisposables) { }
    public virtual void OnViewDeactivation() { }
    public virtual void OnViewDisposal() { }

    #endregion

    #region Equality
    
    public bool Equals(ViewModelBase<TIViewModel>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return InstanceId.Equals(other.InstanceId);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((ViewModelBase<TIViewModel>)obj);
    }

    public override int GetHashCode()
    {
        return InstanceId.GetHashCode();
    }

    public static bool operator ==(ViewModelBase<TIViewModel>? left, ViewModelBase<TIViewModel>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ViewModelBase<TIViewModel>? left, ViewModelBase<TIViewModel>? right)
    {
        return !Equals(left, right);
    }
    
    #endregion
}
