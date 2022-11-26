using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using DDS.Core;
using DDS.Core.Helper;
using DDS.Core.Services;
using DDS.Core.Services.Installer;
using DDS.Core.ViewModels;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace DDS.Avalonia;

public static class Startup
{
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
        servicesAction?.Invoke(services);
        return appBuilder.ConfigureBuilder(services);
    }
    
    private static TAppBuilder ConfigureBuilder<TAppBuilder>(this TAppBuilder appBuilder, IServiceCollection services)
        where TAppBuilder : AppBuilderBase<TAppBuilder>, new()
        => appBuilder.AfterSetup(appBuilderAfterSetup =>
        { 
            // callback after Application has been constructed; Application itself may register services
            // which is why we build the ServiceProvider after it has been built,
            // ServiceProvider.UseMicrosoftDependencyResolver (extension method by Splat) throws exception
            // when BuildServiceProvider before App has been built, order matters

            if (!appBuilderAfterSetup.ApplicationType!.IsAssignableTo(typeof(App)))
                throw new InvalidOperationException("App has to implement IAppCore");
            Globals.ISetGlobalsOnlyOnceOnStartup.InstanceNullable = appBuilderAfterSetup.Instance as App;
            if (Globals.ISetGlobalsOnlyOnceOnStartup.InstanceNullable == null)
                throw new InvalidOperationException("App has to implement IAppCore or Instance is absent, " +
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
            services.AddSingleton(Globals.Instance);
            _ = services.ConfigureAppServiceProvider();
            Globals.ISetGlobalsOnlyOnceOnStartup.FinishGlobalsSetupByMakingGlobalsImmutable();
        });
    
    private static IServiceProvider ConfigureAppServiceProvider(this IServiceCollection services)
    {
        StartupFacade.ConfigureServices(services);
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
        return Globals.Services;
    }
    
    private static IServiceCollection ConfigureAppServices(this IServiceCollection services)
        => services
            .AddSingleton<ServiceScopeManager>()
            .AddLazyResolution()
            .AddScoped<IAvaloniaEssentials,AvaloniaEssentialsCommonService>()
            .AddViewAndViewModels();
    
    //

    private static IServiceCollection AddViewAndViewModels(this IServiceCollection services)
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
        services.AddViewAndViewModels<MainView, MainViewModel>(setDataContext: true)
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
        services.AddViewAndViewModels(ServiceLifetime.Singleton, viewImplFactory, viewModelImplFactory,
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
        services.AddViewAndViewModels(ServiceLifetime.Scoped, viewImplFactory, viewModelImplFactory,
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
        services.AddViewAndViewModels(ServiceLifetime.Transient, viewImplFactory, viewModelImplFactory,
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
    public static IServiceCollection AddViewAndViewModels<TView,TViewModel>(this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<IServiceProvider, TView>? viewImplFactory = default,
        Func<IServiceProvider, TViewModel>? viewModelImplFactory = default,
        Action<IServiceProvider, TView>? postViewCreationAction = default, 
        Action<IServiceProvider, TViewModel>? postViewModelCreationAction = default,
        bool setDataContext = false,
        ServiceLifetime viewLifetime = ServiceLifetime.Transient)
        where TView : ContentControl, IViewFor<TViewModel>
        where TViewModel : class
    {
        // Register ViewModel
        services.Add(ServiceDescriptor.Describe(typeof(TViewModel), p =>
        {
            // p = p.ToScopedWhenScoped(lifetime);
            var viewModel = viewModelImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance<TViewModel>(p);
            // if (viewModel is ViewModelBase viewModelBase) viewModelBase.Services = p;
            postViewModelCreationAction?.Invoke(p, viewModel);
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
            if (setDataContext) view.DataContext = p.GetRequiredService<TViewModel>();
            postViewCreationAction?.Invoke(p, view);
            return view;
        }, viewLifetime));
        
        services.Add(ServiceDescriptor.Describe(typeof(IReactiveViewFor<TViewModel>), 
            p => p.GetRequiredService<TView>(), ServiceLifetime.Transient));

        services.Add(ServiceDescriptor.Describe(typeof(IViewFor<TViewModel>), 
            p => p.GetRequiredService<TView>(), ServiceLifetime.Transient));

        var dict = Globals.ViewModelNameViewTypeDictionary;
        dict[typeof(TViewModel).FullName ?? throw new NullReferenceException()] = typeof(TView);
        
        return services;
    }

    private static IServiceCollection AddWindows(this IServiceCollection services)
        => !Globals.IsClassicDesktopStyleApplicationLifetime 
            ? services : services
                .AddViewAndViewModels<MainWindow, MainWindowViewModel>(setDataContext: true);
}