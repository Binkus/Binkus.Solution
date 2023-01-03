// ReSharper disable MemberCanBePrivate.Global

using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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

/// <summary>
/// <inheritdoc cref="ViewModel{TIViewModel}"/>
/// </summary>
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
        Ioc.Default.GetRequiredService<IServiceScopeManager>().GetCurrentScope().ServiceProvider;
}

/// <summary>
/// Base ViewModel supporting MVVM of ReactiveUI and of CommunityToolkit.Mvvm, like a base class that inherits from
/// ReactiveValidationObject, ObservableRecipient and ObservableValidator, and implements IRoutableViewModel
/// for Navigation, as well as owning a Navigation property. If the ViewModel is a singleton, ignore the Navigation
/// / HostScreen property, they will throw a NotSupportedException as long as you don't provide your own HostScreen
/// during construction. The HostScreen / Navigation property is supposed to be a reference to the NavigationViewModel
/// / NavigationService that implements IScreen, it should be the IScreen which is used to navigate to your ViewModel.
/// By default, if it is not a singleton, it will get the NavigationViewModel of the same ServiceScope.
/// By default a Window is a ServiceScope. So, ViewModels registered with ServiceLifetime.Scoped are basically a
/// Singleton per Window, or per your own scopes. This only works if you provide the Scoped IServiceProvider, which
/// can simply be get during construction by having the IServiceProvider in the constructor of your inherited ViewModel,
/// then pass it to this base class.
/// Additionally it supports asynchronous initialization, override <see cref="InitializeAsync"/> for it to be executed during
/// implementation factory from IoC, right after initialization, the <see cref="OnPrepareAsync"/> method
/// is getting executed.
/// The <see cref="InitializeAsync"/> is only executed once, <see cref="OnPrepareAsync"/> directly after
/// <see cref="InitializeAsync"/> (no activation required),
/// and on each Activation through <see cref="Activator"/>, except for the first one
/// (cause already executed after first Init). Then there is an
/// <see cref="OnActivationAsync"/> method waiting to be overriden that executes always after
/// <see cref="OnPrepareAsync"/> and executes on every Activation through <see cref="Activator"/>.
/// Basically someIServiceProvider.GetRequiredService(typeof(YourViewModel)) or injecting that VM through
/// a constructor that gets resolved by DI, this will asynchronously execute <see cref="InitializeAsync"/> before e.g.
/// that GetRequiredService returns the ViewModel. PrepareAsync will await the Initialization Task, and ActivateAsync
/// will await the Prepare Task; as long as the asynchronous Initialization (and Prepare/Activate) is running,
/// the IsActivated property is false. This can be used e.g. by the RoutedViewHost, to disable the controls during
/// activation. The Initialize, Prepare and Activation Task are truly asynchronous as long as the properties
/// <see cref="JoinInitBeforeOnActivationFinished"/> / <see cref="JoinPrepareBeforeOnActivationFinished"/>
/// / <see cref="JoinActivationBeforeOnActivationFinished"/>
/// stay set to false. The Initialize Task and (the first) Prepare Task are truly asynchronous even with
/// Join*BeforeOnActivationFinished until the ViewModel gets activated, activation will then join these Tasks,
/// which basically means synchronously awaiting it. These tasks are all created with a <see cref="JoinableTaskFactory"/>
/// to not cause any deadlocks. All of these Tasks are running/initiated on the Main/UI Thread.
/// If you want to lift of sth. within e.g. <see cref="InitializeAsync"/> to a ThreadPoolThread, use usual TPL, e.g.
/// await Task.Run(...), or awaiting any Task from any Lib, or wrapping those Tasks in a Task.Run.
/// As you know by default most of TPL is about asynchronousity not about parallelism, parallelism comes with e.g. that
/// mentioned special function Task.Run which can be asynchronously awaited which captures the SynchronizationContext
/// as long as you don't use someTask.ConfigureAwait(false). Do not use someTask.ConfigureAwait(false) within
/// any of the Initialize methods, or know what you are doing by e.g. not manipulating any UI related properties after
/// running *.ConfigureAwait(false). Without *.ConfigureAwait(false) you are guaranteed to be on Main UI Thread after
/// each await (of a Task). As you know, Tasks that get awaited can internally do stuff on any thread, which you do not
/// have to care about, because after the returned result it will automatically restore your SynchronizationContext
/// (switch back to UI thread).
/// <seealso cref="ReactiveValidationObservableRecipientValidator"/>
/// <seealso cref="ReactiveObservableObject"/>
/// <seealso cref="ObservableObject"/>
/// <seealso cref="ObservableValidator"/>
/// <seealso cref="ReactiveObject"/>
/// <seealso cref="ReactiveUI.Validation.Helpers.ReactiveValidationObject"/>
/// <seealso cref="Activator"/>
/// <seealso cref="ViewModelActivator"/>
/// <seealso cref="ViewForMixins.WhenActivated(ReactiveUI.IActivatableViewModel,Action{CompositeDisposable})"/>
/// <seealso cref="ViewForMixins.WhenActivated(ReactiveUI.IActivatableViewModel,System.Func{System.Collections.Generic.IEnumerable{System.IDisposable}})"/>
/// <seealso cref="JoinableTaskFactory"/>
/// <seealso cref="JoinableTask"/>
/// <seealso cref="InitializeAsync"/> 
/// <seealso cref="OnPrepareAsync"/>
/// <seealso cref="OnActivation"/>
/// <seealso cref="OnActivationAsync"/>
/// <seealso cref="OnActivationFinishing"/>
/// <seealso cref="OnDeactivation"/>
/// <seealso cref="RegisterAllMessagesOnActivation"/>
/// <seealso cref="EnableAsyncInitPrepareActivate"/>
/// </summary>
/// <typeparam name="TIViewModel">Ignore: Not used yet. Inherit from ViewModel without generic param instead.</typeparam>
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
            IsCurrentlyActivating = true;
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
            IsCurrentlyActivating = false;
        });
        
        Debug.WriteLine("c:"+ViewModelName);
    }

    private void HandleAsyncActivation(CompositeDisposable disposables, bool isDisposed, CancellationToken token)
    {
        if (!IsInitInitiated) return;
        
        if (isDisposed)
        {
            Prepare = JoinUiTaskFactory.RunAsync(() => OnPrepareAsync(PrepDisposables, token));
            
            // todo rm
            // if (!JoinPrepareBeforeOnActivationFinished)
            // {
            //     // This ensures Exceptions get thrown
            //     RxApp.TaskpoolScheduler.Schedule(Prepare, (joinTask,_) => joinTask?.Join());
            // }
        }
            
        Activation = JoinUiTaskFactory.RunAsync(() => OnActivationBaseAsync(disposables, token));
        
        // todo rm
        // if (!JoinActivationBeforeOnActivationFinished)
        // {
        //     // This ensures Exceptions get thrown
        //     RxApp.TaskpoolScheduler.Schedule(Activation, (joinTask,_) => joinTask?.Join());
        // }

        //
        
        // var t0 = 0.AddTimestamp();
        // InitPrepareActivation = JoinUiTaskFactory.RunAsync(async () =>
        // {
        //     var t = 0.AddTimestamp();
        //     var initEx = await Init!.IgnoreExceptionAsync<OperationCanceledException>().TryAwaitAsync<Exception>();
        //     var prepEx = await Prepare!.IgnoreExceptionAsync<OperationCanceledException>().TryAwaitAsync<Exception>();
        //     var activeEx = await Activation!.IgnoreExceptionAsync<OperationCanceledException>().TryAwaitAsync<Exception>();
        //     List<ExceptionDispatchInfo> exceptions = new(3);
        //     initEx.IfNotNullAddTo(exceptions);
        //     prepEx.IfNotNullAddTo(exceptions);
        //     activeEx.IfNotNullAddTo(exceptions);
        //     if (exceptions.Count > 0) throw new AggregateException(exceptions.Select(x => x.SourceException));
        //     t.LogTime("InitPrepareActivation:"+ViewModelName);
        // });
        // t0.LogTime("XInitPrepareActivationX+"+ViewModelName);
    }

    [IgnoreDataMember] protected JoinableTask? Init { get; private set; }
    
    [IgnoreDataMember] protected JoinableTask? Prepare { get; private set; }
    
    [IgnoreDataMember] protected JoinableTask? Activation { get; private set; }
    
    // [IgnoreDataMember] protected JoinableTask? InitPrepareActivation { get; private set; }

    [IgnoreDataMember] public bool IsInitInitiated { get; private set; }
    
    void IInitializable.Initialize(CancellationToken cancellationToken)
    {
        if (!EnableAsyncInitPrepareActivate || IsInitInitiated) return;
        
        IsInitInitiated = true;
        
        Init = JoinUiTaskFactory.RunAsync(async () =>
        {
            await JoinUiTaskFactory.SwitchToMainThreadAsync(true);
            await InitializeAsync(cancellationToken);
        });
        
        // todo rm
        // if (!JoinInitBeforeOnActivationFinished)
        // {
        //     // This ensures Exceptions get thrown
        //     RxApp.TaskpoolScheduler.Schedule(Init, (joinTask,_) =>
        //     {
        //         joinTask?.Join();
        //         // try
        //         // {
        //         //     joinTask?.Join();
        //         // }
        //         // catch (Exception e)
        //         // {
        //         //     var ex = ExceptionDispatchInfo.Capture(e);
        //         //     RxApp.MainThreadScheduler.Schedule(Unit.Default, (_, _) => ex.Throw());//new Exception(e.StackTrace, e));
        //         //     // throw;
        //         // }
        //     });
        // }
        
        // var handle = cancellationToken.WaitHandle;
        // todo ActivationCancellationTokenSource.Cancel() when cancellationToken gets canceled
        
        Prepare = JoinUiTaskFactory.RunAsync(() =>
            OnPrepareBaseAsync(PrepDisposables, ActivationCancellationTokenSource.Token));
        
        // todo rm
        // if (!JoinPrepareBeforeOnActivationFinished)
        // {
        //     // This ensures Exceptions get thrown
        //     RxApp.TaskpoolScheduler.Schedule(Prepare, (joinTask,_) => joinTask?.Join());
        // }
    }

    protected virtual Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    
    private async Task OnPrepareBaseAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        if (Init is { } init) await init;//.IgnoreExceptionAsync<Exception>();
        await JoinUiTaskFactory.SwitchToMainThreadAsync(true);
        await OnPrepareAsync(disposables, cancellationToken).IgnoreExceptionAsync<OperationCanceledException>();
    }

    protected virtual Task OnPrepareAsync(CompositeDisposable disposables, CancellationToken cancellationToken) =>
        Task.CompletedTask;
    
    private async Task OnActivationBaseAsync(CompositeDisposable disposables, CancellationToken cancellationToken)
    {
        ExceptionDispatchInfo? exceptionDispatchInfo = null;
        try
        {
            if (Init is { } init) await init; //.IgnoreExceptionAsync<Exception>();
            if (Prepare is { } prepare) await prepare; //.IgnoreExceptionAsync<Exception>();
            
            await JoinUiTaskFactory.SwitchToMainThreadAsync(true);

            await OnActivationAsync(disposables, cancellationToken);
            // await OnActivationAsync(disposables, cancellationToken).IgnoreExceptionAsync<OperationCanceledException>();

            // var e = await OnActivationAsync(disposables, cancellationToken)
            //     .IgnoreExceptionAsync<OperationCanceledException>()
            //     .TryAwaitAsync<Exception>();
            // e?.Throw();
        }
        catch (OperationCanceledException) { /* ignored */ }
        catch (Exception e)
        {
            exceptionDispatchInfo = ExceptionDispatchInfo.Capture(e);
            await JoinUiTaskFactory.SwitchToMainThreadAsync();
            
            // todo Evaluate awaiting some safe AppShutdown task, e.g. for safely closing Db connection and so on.
            
            // Crashing app with correct StackTrace with first exception:
            ThreadPool.QueueUserWorkItem(_ => exceptionDispatchInfo.Throw());
            await RxApp.MainThreadScheduler.Yield();
        }
        finally
        {
            if (exceptionDispatchInfo is not null)
            {
                // ThreadPool.QueueUserWorkItem above will crash the app, we are intentionally waiting for the it, cause
                // the Async-Init API consumer forgot catching her/*/his errors while e.g. overriding InitializeAsync:
                await RxApp.MainThreadScheduler.Yield();
                await RxApp.TaskpoolScheduler.Yield();
                await RxApp.MainThreadScheduler.Yield();
                Environment.FailFast(
                    #if DEBUG
                    exceptionDispatchInfo.SourceException.StackTrace,
                    #else
                    exceptionDispatchInfo.SourceException.Message,
                    #endif
                    exceptionDispatchInfo.SourceException);
                exceptionDispatchInfo.Throw(); // should never be hit
            }
            
            await JoinUiTaskFactory.SwitchToMainThreadAsync(true);
            OnActivationFinishing(disposables, cancellationToken);
            TrySetActivated(disposables, cancellationToken);
            IsCurrentlyActivating = false;
            
            // e?.Throw();
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TrySetActivated(ICancelable cancelable, CancellationToken token = default) => IsActivated = 
        !((token.IsCancellationRequested || cancelable.IsDisposed || PrepDisposables.IsDisposed) && !IsActivated);
    
    private void SetDeactivated()
    {
        IsCurrentlyActivating = false;
        IsActivated = false;
        IsActive = false;
    }

    private bool _isActivated, _isCurrentlyActivating;
    [IgnoreDataMember] public bool IsActivated { get => _isActivated;
        private set => this.RaiseAndSetIfChanged(ref _isActivated, value); }
    [IgnoreDataMember] public bool IsCurrentlyActivating { get => _isCurrentlyActivating;
        private set => this.RaiseAndSetIfChanged(ref _isCurrentlyActivating, value); }

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
    //     // RxApp.MainThreadScheduler.ScheduleAsync(InitPrepareActivation, async (a, b, c) =>
    //     // {
    //     //     await b!;
    //     // });
    //     
    //     OnActivationFinishing(disposables, cancellationToken);
    // }

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

        var isActivated = IsActivated; // (currently) not needed up here
        var isCurrentlyActivating = IsCurrentlyActivating; // needed up here
        
        var tokenSource = ActivationCancellationTokenSource;
        var cancelled = tokenSource.Token.IsCancellationRequested;
        if (!cancelled) // when cancelled the following inner-if-block already ran
        {
            try { tokenSource.Cancel(); }
            catch (Exception) { /* ignore */ }
            finally { JoinAsyncInitTasksAndDispose(tokenSource); }
        }
        
        if (!isCurrentlyActivating && !isActivated)
        {
            SetDeactivated();
            return;
        }
        SetDeactivated();

        if (!cancelled) // prevents running OnDeactivation twice while same Activation cycle
            OnDeactivation();
    }

    private void JoinAsyncInitTasksAndDispose(IDisposable disposable)
    {
        try
        {
            if (!IsInitInitiated) return;
            // JoinUiTaskFactory.Run(async () =>
            // {
            //     // await InitPrepareActivation!;
            //     
            //     // if (Init is { } init) await init.IgnoreExceptionAsync<Exception>();
            //     // if (Prepare is { } prepare) await prepare.IgnoreExceptionAsync<Exception>();
            //     // if (Activation is { } activation) await activation.IgnoreExceptionAsync<Exception>();
            // });
            
            // todo evaluate adding potential application CancellationToken (e.g. triggered OnAppShutdown / window close)
            // InitPrepareActivation?.Join();
            Activation?.Join();
        }
        finally
        {
            disposable.Dispose();
        }
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

    #region ICancelable IDisposable IAsyncDisposable
    
    // Optional base impl for Disposable interface, if inherited implements e.g. IDisposable
    // (e.g. for collections of VMs, they all can call Dispose even when not implementing IDisposable)

    /// <inheritdoc cref="ICancelable.IsDisposed" />
    public bool IsDisposed { get; private set; }
    
    /// <summary>
    /// Used for sync disposal, called by Dispose() with disposing set to true,
    /// called by finalizer with disposing set to false.
    /// <inheritdoc cref="System.IDisposable.Dispose"/>
    /// </summary>
    /// <param name="disposing"><inheritdoc cref="DisposeShared"/></param>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>
    /// Cancels Init, Prepare and Activation tasks, calls OnDeactivation(), Dispose(true) and DisposeShared(true).
    /// Override DisposeAsync(bool) and Dispose(bool),
    /// or just DisposeShared(bool) which is called by both Dispose and DisposeAsync.
    /// <inheritdoc cref="System.IDisposable.Dispose"/>
    /// </summary>
    /// <inheritdoc cref="System.IDisposable.Dispose"/>
    public void Dispose()
    {
        if (IsDisposed) return;
        Activator.Dispose();
        OnDeactivationBase();
        DisposeShared(true);
        Dispose(true);
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
    
    /// <summary>
    /// Used for sync disposal, shared between Dispose() DisposeAsync() (both will supply true as param), finalizer
    /// calls this with disposing = false.
    /// </summary>
    /// <param name="disposing">true when calling Dispose() or DisposeAsync() for releasing managed resources,
    /// false if called by finalizer (GC) - for releasing unmanaged resources. Usually manged resources don't
    /// have to be disposed, so this is primarily for unmanaged resources. But e.g. System.Reactive highly depends
    /// on IDisposable interfaces, used to cancel e.g. subscriptions. It can be good practise to dispose them
    /// to prevent potential memory leaks. Means: IDisposable can be used for more than just releasing unmanaged
    /// resources. Use this to dispose subscriptions that may not get unsubscribed when GC runs, or that could
    /// prevent GC from collecting. When false, you only have to release unmanaged resources, when true
    /// release unmanaged resources and dispose e.g. subscriptions.</param>
    protected virtual void DisposeShared(bool disposing) { }

    /// <summary>
    /// Used for async disposal, called by DisposeAsync().
    /// <inheritdoc cref="System.IAsyncDisposable.DisposeAsync"/>
    /// </summary>
    /// <param name="disposing">dummy param</param>
    /// <inheritdoc cref="System.IAsyncDisposable.DisposeAsync"/>
    protected virtual ValueTask DisposeAsync(bool disposing) => default;

    /// <summary>
    /// Cancels Init, Prepare and Activation tasks, calls OnDeactivation(), DisposeShared(true) and DisposeAsync(true).
    /// Override DisposeAsync(bool) and Dispose(bool),
    /// or just DisposeShared(bool) which is called by both Dispose and DisposeAsync.
    /// <inheritdoc cref="System.IAsyncDisposable.DisposeAsync"/>
    /// </summary>
    /// <inheritdoc cref="System.IAsyncDisposable.DisposeAsync"/>
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed) return;
        Activator.Dispose();
        OnDeactivationBase();
        DisposeShared(true);
        await DisposeAsync(true).ConfigureAwait(false);
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Calls DisposeShared(false) and Dispose(false).
    /// <inheritdoc />
    /// </summary>
    ~ViewModel()
    {
        DisposeShared(false);
        Dispose(false);
    }
    
    #endregion
}
