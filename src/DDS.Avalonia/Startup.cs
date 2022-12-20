using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using DDS.Core;
using DDS.Core.Controls;
using DDS.Core.Helper;
using DDS.Core.Services;
using DDS.Core.Services.Installer;
using DDS.Core.ViewModels;
using Microsoft.VisualStudio.Threading;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace DDS.Avalonia;

public static class Startup
{
    public static long StartTimestamp { get; }

    static Startup()
    {
        StartTimestamp = 0.AddTimestamp();
    }

    public static TAppBuilder ConfigureAppServices<TAppBuilder>(
        this TAppBuilder appBuilder, Action<IServiceCollection>? servicesAction = default
    )
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        return appBuilder.ConfigureAppServices(new ServiceCollection(), servicesAction);
    }

    private static TAppBuilder ConfigureAppServices<TAppBuilder>(
        this TAppBuilder appBuilder,  IServiceCollection services, Action<IServiceCollection>? servicesAction
    )
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
    {
        Globals.ISetGlobalsOnlyOnceOnStartup.IsDesignMode = Design.IsDesignMode;
        services.UseMicrosoftDependencyResolver();
        appBuilder.UseReactiveUI(); // Most of ReactiveUI is initialized here already, so DI additions below here:
        StartTimestamp.LogTime<PerformanceLogger.StartupPerformance>().Save();
        var time = 0.AddTimestamp();
        servicesAction?.Invoke(services);
        appBuilder.ConfigureBuilder(services);
        time.LogTime<PerformanceLogger.StartupConfigureBuilderPerformance>().Save();
        return appBuilder;
    }
    
    private static TAppBuilder ConfigureBuilder<TAppBuilder>(this TAppBuilder appBuilder, IServiceCollection services)
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        => appBuilder.AfterSetup(appBuilderAfterSetup =>
        {
            var time = 0.AddTimestamp();
            // callback after Application has been constructed; Application itself may register services
            // which is why we build the ServiceProvider after it has been built,
            // ServiceProvider.UseMicrosoftDependencyResolver (extension method by Splat) throws exception
            // when BuildServiceProvider before App has been built, order matters

            if (!appBuilderAfterSetup.ApplicationType!.IsAssignableTo(typeof(App)))
                throw new InvalidOperationException("App has to implement IAppCore");
            
            Globals.ISetGlobalsOnlyOnceOnStartup.InstanceNullable = appBuilderAfterSetup.Instance 
                as App ?? throw new InvalidOperationException("App has to implement IAppCore or Instance is absent, " +
                                                              "consider checking breaking changes of recent Avalonia updates");
            
            if (!Globals.IsDesignMode) // => appBuilderAfterSetup.Instance.ApplicationLifetime is null when IsDesignMode
            {
                Globals.ISetGlobalsOnlyOnceOnStartup.ApplicationLifetime = appBuilderAfterSetup.Instance?.ApplicationLifetime
                                                          ?? throw new InvalidOperationException(
                                                              "Instance of Application or ApplicationLifetime is null");
                
                Globals.ISetGlobalsOnlyOnceOnStartup.IsClassicDesktopStyleApplicationLifetime =
                    appBuilderAfterSetup.Instance.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime;
                
                Globals.ISetGlobalsOnlyOnceOnStartup.ApplicationLifetimeWrapped =
                    Globals.IsClassicDesktopStyleApplicationLifetime
                        ? new DesktopLifetimeWrapper(
                            (IClassicDesktopStyleApplicationLifetime)Globals.ApplicationLifetime) 
                        : new SingleViewLifetimeWrapper((ISingleViewApplicationLifetime)Globals.ApplicationLifetime);
            }
            services.AddSingleton<IAppCore>(Globals.Instance);
            
            Globals.ISetGlobalsOnlyOnceOnStartup.JoinUiTaskFactory = new JoinableTaskFactory(new JoinableTaskContext());
            services.AddSingleton<JoinableTaskFactory>(Globals.JoinUiTaskFactory);
            
            _ = services.ConfigureAppServiceProvider(); // => kick-starting all our registrations
            
            Globals.ISetGlobalsOnlyOnceOnStartup.FinishGlobalsSetupByMakingGlobalsImmutable();
            
            // time.LogTime("AfterSetup time (includes DI time) ");
            var diTime = PerformanceLogger.TryGetResult<PerformanceLogger.DependencyInjectionPerformance>();
            time.GetElapsedTime().Subtract(diTime ?? 0.Ticks()).LogTime<PerformanceLogger.AfterSetupPerformance>().Save();
        });
    
    private static IServiceProvider ConfigureAppServiceProvider(this IServiceCollection services)
    {
        var time = 0.AddTimestamp();
        StartupFacade.ConfigureServices(services); // => kick-starting all our registrations
        _ = services.ConfigureAppServices(); // => kick-starting all our registrations
#if DEBUG
        Globals.ISetGlobalsOnlyOnceOnStartup.ServiceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true } );
#else
        Globals.ISetGlobalsOnlyOnceOnStartup.ServiceProvider = services.BuildServiceProvider();
#endif
        Globals.Services.UseMicrosoftDependencyResolver();

        if (Globals.IsDesignMode)
        {
            // Skip DB Migration when Design Mode
            Globals.ISetGlobalsOnlyOnceOnStartup.DbMigrationTask = Task.CompletedTask;
            return Globals.Services;
        }
        
        // // Future DB setup
        // SQLitePCL.Batteries_V2.Init();
        Globals.ISetGlobalsOnlyOnceOnStartup.DbMigrationTask = Task.Run(() =>
        {
            // ExecuteOnServiceProviderCreation.Clear();
            // ExecuteOnServiceProviderCreation = null;
            // using var scope = IAppCore.ServiceProvider.CreateScope();
            // using var context = scope.ServiceProvider.GetRequiredService<AvaloniaDbContext>();
            // return context.Database.MigrateAsync();
            // ReSharper disable once ConvertToLambdaExpression
            return Task.CompletedTask;
        });
        time.LogTime<PerformanceLogger.DependencyInjectionPerformance>().Save();
        return Globals.Services;
    }
    
    private static IServiceCollection ConfigureAppServices(this IServiceCollection services)
        => services
            .AddSingleton<ServiceScopeManager>()
            .AddLazyResolution()
            .AddScoped<IAvaloniaEssentials,AvaloniaEssentialsCommonService>()
            .AddViewsAndViewModels();
    
    //

    private static IServiceCollection AddViewsAndViewModels(this IServiceCollection services)
    {
        services
            .AddScoped<TopLevelService>()
            .AddSingleton<IViewLocator, ReactiveViewLocator>()
            .AddSingleton<ApplicationViewModel>();
        // services.Add(ServiceDescriptor.Describe(typeof(IReactiveWindowFor<>), 
        //     typeof(BaseWindow<>), ServiceLifetime.Transient));
        // services.Add(ServiceDescriptor.Describe(typeof(IReactiveViewFor<>), 
        //     typeof(BaseUserControl<>), ServiceLifetime.Transient));
        // services.Add(ServiceDescriptor.Describe(typeof(IViewFor<>),
        //     typeof(BaseUserControl<>), ServiceLifetime.Transient));
        // services.Add(ServiceDescriptor.Describe(typeof(IViewModelBase), 
        //     typeof(ViewModelBase), ServiceLifetime.Transient));    
        services.AddViewViewModel<MainView, MainViewModel>(setDataContext: true)
            .AddScoped(typeof(INavigationViewModel<>), typeof(NavigationViewModel<>))
            // .AddTransient<INavigationViewModel, INavigationViewModel<INavigationViewModel>>(p => 
            //     p.GetRequiredService<INavigationViewModel<INavigationViewModel>>())
            .AddScoped<INavigationViewModel, NavigationViewModel>()
            .AddTransient<IScreen, INavigationViewModel>(p => 
                p.GetRequiredService<INavigationViewModel>())
            // .AddViewAndViewModels<SecondTestView,SecondTestViewModel>(ServiceLifetime.Singleton)
            // .AddViewAndViewModels<TestView,TestViewModel>(ServiceLifetime.Singleton)
            ;
        StartupFacade.ConfigureViewViewModels(services);
        services.AddWindows();
        StartupFacade.ConfigureWindowViewModels(services);
        return services;
    }

    public static IServiceCollection AddSingletonViewViewModel<TView, TViewModel>(this IServiceCollection services,
        Func<IServiceProvider, TView>? viewImplFactory = default,
        Func<IServiceProvider, TViewModel>? viewModelImplFactory = default,
        Action<IServiceProvider, TView>? postViewCreationAction = default,
        Action<IServiceProvider, TViewModel>? postViewModelCreationAction = default,
        bool setDataContext = false,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient)
        where TView : ContentControl, IViewFor<TViewModel>
        where TViewModel : class =>
        services.AddViewViewModel(ServiceLifetime.Singleton, viewImplFactory, viewModelImplFactory,
            postViewCreationAction, postViewModelCreationAction, setDataContext, viewLifetime);
    
    public static IServiceCollection AddScopedViewViewModel<TView, TViewModel>(this IServiceCollection services,
        Func<IServiceProvider, TView>? viewImplFactory = default,
        Func<IServiceProvider, TViewModel>? viewModelImplFactory = default,
        Action<IServiceProvider, TView>? postViewCreationAction = default,
        Action<IServiceProvider, TViewModel>? postViewModelCreationAction = default,
        bool setDataContext = false,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient)
        where TView : ContentControl, IViewFor<TViewModel>
        where TViewModel : class =>
        services.AddViewViewModel(ServiceLifetime.Scoped, viewImplFactory, viewModelImplFactory,
            postViewCreationAction, postViewModelCreationAction, setDataContext, viewLifetime);
    
    public static IServiceCollection AddTransientViewViewModel<TView, TViewModel>(this IServiceCollection services,
        Func<IServiceProvider, TView>? viewImplFactory = default,
        Func<IServiceProvider, TViewModel>? viewModelImplFactory = default,
        Action<IServiceProvider, TView>? postViewCreationAction = default,
        Action<IServiceProvider, TViewModel>? postViewModelCreationAction = default,
        bool setDataContext = false,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient)
        where TView : ContentControl, IViewFor<TViewModel>
        where TViewModel : class =>
        services.AddViewViewModel(ServiceLifetime.Transient, viewImplFactory, viewModelImplFactory,
            postViewCreationAction, postViewModelCreationAction, setDataContext, viewLifetime);

    /// <summary>
    /// Registers View and ViewModel and match them together for Navigation through ReactiveViewLocator.
    /// <p>Default Scope of IServiceProvider is used for first instance of MainViewModel, so scoped for each Main instance.</p>
    /// </summary>
    /// <param name="services">IServiceCollection to register to, which is used to build the IServiceProvider.</param>
    /// <param name="lifetime">ServiceLifetime of ViewModel, recommended are Scoped and Transient.</param>
    /// <param name="viewImplFactory">Default is <code>ActivatorUtilities.CreateInstance&lt;TView&gt;</code>
    /// Recommended to not change default until required.</param>
    /// <param name="viewModelImplFactory">Default is <code>ActivatorUtilities.CreateInstance&lt;TView&gt;</code>
    /// Recommended to not change default until required.</param>
    /// <param name="postViewCreationAction">Default is do nothing</param>
    /// <param name="postViewModelCreationAction">Default is do nothing</param>
    /// <param name="setDataContext">When true sets the DataContext after View resolvation.</param>
    /// <param name="viewLifetime">ServiceLifetime of View - highly recommended to stay default transient.</param>
    /// <typeparam name="TView">View type, ContentControl and IViewFor&lt;TViewModel&gt;</typeparam>
    /// <typeparam name="TViewModel">ViewModel type</typeparam>
    /// <returns><see cref="services"/></returns>
    /// <exception cref="NullReferenceException"></exception>
    public static IServiceCollection AddViewViewModel<TView,TViewModel>(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<IServiceProvider, TView>? viewImplFactory = default,
        Func<IServiceProvider, TViewModel>? viewModelImplFactory = default,
        Action<IServiceProvider, TView>? postViewCreationAction = default, 
        Action<IServiceProvider, TViewModel>? postViewModelCreationAction = default,
        bool setDataContext = false,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient)
        where TView : class, IViewFor<TViewModel>
        where TViewModel : class
    {
        // return services.AddViewViewModel(typeof(TView), typeof(TViewModel), lifetime, viewImplFactory, viewModelImplFactory,
        //     (p, o) => postViewCreationAction?.Invoke(p, (TView)o), 
        //     (p, o) => postViewModelCreationAction?.Invoke(p, (TViewModel)o), 
        //     setDataContext, viewLifetime);
        
        // Register ViewModel
        services.Add(ServiceDescriptor.Describe(typeof(TViewModel), p =>
        {
            // p = p.ToScopedWhenScoped(lifetime);
            var viewModel = viewModelImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance<TViewModel>(p);
            // if (viewModel is ViewModelBase viewModelBase) viewModelBase.Services = p;
            postViewModelCreationAction?.Invoke(p, viewModel);
            if (viewModel is IInitializable initializable)
                initializable.InitializeOnceAfterCreation(default);
            return viewModel;
        }, lifetime));

        // Register View
        // Views should be Transient when not [SingleInstanceView], cause Singleton or Scoped Views
        // (when acting like Singleton on Global Main Scope) can be buggy except for MainView and MainWindow,
        // ViewModels can have any ServiceLifetime; with buggy I mean the navigation of ReactiveUI can be partially
        // broken, that it simply does not show the view on 2nd navigation to it, but shows again on 3rd navigation,...
        
        services.Add(ServiceDescriptor.Describe(typeof(TView), p =>
        {
            var view = viewImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance<TView>(p);
            // if (view is BaseUserControl<TViewModel> baseUserControl) baseUserControl.DisposeOnDeactivation = true;
            if (setDataContext) view.ViewModel = p.GetRequiredService<TViewModel>();
            if (viewLifetime is ServiceLifetime.Transient && view is ICoreView cw)
                cw.DisposeWhenActivatedSubscription = true;
            postViewCreationAction?.Invoke(p, view);
            return view;
        }, viewLifetime));
        
        // services.Add(ServiceDescriptor.Describe(typeof(IReactiveViewFor<TViewModel>), 
        //     p => p.GetRequiredService<TView>(), ServiceLifetime.Transient));

        services.Add(ServiceDescriptor.Describe(typeof(IViewFor<TViewModel>), 
            p => p.GetRequiredService<TView>(), ServiceLifetime.Transient));

        var dict = Globals.ViewModelNameViewTypeDictionary;
        dict[typeof(TViewModel).FullName ?? throw new NullReferenceException()] = typeof(TView);

        // services.AddSingleton<LifetimeOf<TView>>(new LifetimeOf<TView>(viewLifetime));
        services.AddSingleton<LifetimeOf<TViewModel>>(new LifetimeOf<TViewModel>(lifetime));
        
        return services;
    }
    
    /// <summary>
    /// Registers View and ViewModel and match them together for Navigation through ReactiveViewLocator.
    /// <p>Default Scope of IServiceProvider is used for first instance of MainViewModel, so scoped for each Main instance.</p>
    /// </summary>
    /// <param name="services">IServiceCollection to register to, which is used to build the IServiceProvider.</param>
    /// <param name="viewType">Type of View which inherits from BaseUserControl or BaseWindow</param>
    /// <param name="viewModelType">Type of ViewModel</param>
    /// <param name="lifetime">ServiceLifetime of ViewModel, recommended are Scoped and Transient.</param>
    /// <param name="viewImplFactory">Default is <code>ActivatorUtilities.CreateInstance&lt;TView&gt;</code>
    /// Recommended to not change default until required.</param>
    /// <param name="viewModelImplFactory">Default is <code>ActivatorUtilities.CreateInstance&lt;TView&gt;</code>
    /// Recommended to not change default until required.</param>
    /// <param name="postViewCreationAction">Default is do nothing</param>
    /// <param name="postViewModelCreationAction">Default is do nothing</param>
    /// <param name="setDataContext">When true sets the DataContext after View resolvation.</param>
    /// <param name="viewLifetime">ServiceLifetime of View - highly recommended to stay default transient.</param>
    /// <returns><see cref="services"/></returns>
    /// <exception cref="NullReferenceException"></exception>
    public static IServiceCollection AddViewViewModel(this IServiceCollection services, Type viewType, Type viewModelType,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<IServiceProvider, object>? viewImplFactory = default,
        Func<IServiceProvider, object>? viewModelImplFactory = default,
        Action<IServiceProvider, object>? postViewCreationAction = default, 
        Action<IServiceProvider, object>? postViewModelCreationAction = default,
        bool setDataContext = false,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient)
    {
#if DEBUG
        if (!viewType.IsAssignableTo(typeof(BaseUserControl<>).MakeGenericType(viewModelType)) &&
            !viewType.IsAssignableTo(typeof(BaseWindow<>).MakeGenericType(viewModelType)))
            throw new InvalidOperationException();
#endif
        
        // Register ViewModel
        services.Add(ServiceDescriptor.Describe(viewModelType, p =>
        {
            // p = p.ToScopedWhenScoped(lifetime);
            var viewModel = viewModelImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance(p, viewModelType);
            // if (viewModel is ViewModelBase viewModelBase) viewModelBase.Services = p;
            postViewModelCreationAction?.Invoke(p, viewModel);
            if (viewModel is IInitializable initializable)
                initializable.InitializeOnceAfterCreation(default);
            return viewModel;
        }, lifetime));

        // Register View
        // Views should be Transient when not [SingleInstanceView], cause Singleton or Scoped Views
        // (when acting like Singleton on Global Main Scope) can be buggy except for MainView and MainWindow,
        // ViewModels can have any ServiceLifetime; with buggy I mean the navigation of ReactiveUI can be partially
        // broken, that it simply does not show the view on 2nd navigation to it, but shows again on 3rd navigation,...
        
        services.Add(ServiceDescriptor.Describe(viewType, p =>
        {
            var view = viewImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance(p, viewType);
            // if (view is BaseUserControl<TViewModel> baseUserControl) baseUserControl.DisposeOnDeactivation = true;
            if (setDataContext && view is IViewFor v) v.ViewModel = p.GetRequiredService(viewModelType);
            if (viewLifetime is ServiceLifetime.Transient && view is ICoreView cw)
                cw.DisposeWhenActivatedSubscription = true;
            postViewCreationAction?.Invoke(p, view);
            return view;
        }, viewLifetime));
        
        // services.Add(ServiceDescriptor.Describe(typeof(IReactiveViewFor<>).MakeGenericType(viewModelType), 
        //     p => p.GetRequiredService(viewType), ServiceLifetime.Transient));

        services.Add(ServiceDescriptor.Describe(typeof(IViewFor<>).MakeGenericType(viewModelType), 
            p => p.GetRequiredService(viewType), ServiceLifetime.Transient));

        var dict = Globals.ViewModelNameViewTypeDictionary;
        dict[viewModelType.FullName ?? throw new NullReferenceException()] = viewType;

        // var lifetimeOfVType = typeof(LifetimeOf<>).MakeGenericType(viewType);
        // services.AddSingleton(lifetimeOfVType, Activator.CreateInstance(lifetimeOfVType, viewLifetime)!);
        
        var lifetimeOfVmType = typeof(LifetimeOf<>).MakeGenericType(viewModelType);
        services.AddSingleton(lifetimeOfVmType, Activator.CreateInstance(lifetimeOfVmType, lifetime)!);
        
        return services;
    }

    private static IServiceCollection AddWindows(this IServiceCollection services)
        => !Globals.IsClassicDesktopStyleApplicationLifetime 
            ? services : services
                .AddViewViewModel<MainWindow, MainWindowViewModel>(setDataContext: true);
}