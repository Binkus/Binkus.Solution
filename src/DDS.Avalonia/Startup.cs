using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using DDS.Core;
using DDS.Core.Services;
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
            _ = services.ConfigureAppServiceProvider();
            Globals.ISetGlobalsOnlyOnceOnStartup.FinishGlobalsSetupByMakingGlobalsImmutable();
        });
    
    private static IServiceProvider ConfigureAppServiceProvider(this IServiceCollection services)
    {
        StartupFacade.ConfigureServices(services);
        _ = services.ConfigureAppServices(); // => kick-starting all our registrations
        Globals.ISetGlobalsOnlyOnceOnStartup.ServiceProvider = services.BuildServiceProvider();
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
            .AddTaskResolution()
            .AddLazyResolution()
            .AddSingleton<IAvaloniaEssentials,AvaloniaEssentialsCommonService>()
            .AddViewAndViewModels();
    
    private static IServiceCollection AddLazyResolution(this IServiceCollection services) 
        => services.AddTransient(
            typeof(Lazy<>),
            typeof(LazilyResolved<>));

    private sealed class LazilyResolved<T> : Lazy<T> where T : notnull
    {
        public LazilyResolved(IServiceProvider serviceProvider)
            : base(serviceProvider.GetRequiredService<T>)
        {
        }
    }

    private static IServiceCollection AddTaskResolution(this IServiceCollection services)
        => services.AddTransient(typeof(Task<>), typeof(TaskResolved<>));
            
    private sealed class TaskResolved<T> : Task<T> where T : notnull
    {
        public TaskResolved(IServiceProvider serviceProvider)
            : base(serviceProvider.GetRequiredService<T>)
        {
        }
    }

    private static IServiceCollection AddViewAndViewModels(this IServiceCollection services)
    {
        services
            .AddScoped<TopLevelService>()
            .AddSingleton<IViewLocator, ReactiveViewLocator>()
            .AddSingleton<ApplicationViewModel>()
            .AddSingleton<NavigationViewModel>()
            .AddSingleton<IScreen, NavigationViewModel>(p => p.GetRequiredService<NavigationViewModel>())
            // .AddViewAndViewModels<SecondTestView,SecondTestViewModel>(ServiceLifetime.Singleton)
            // .AddViewAndViewModels<TestView,TestViewModel>(ServiceLifetime.Singleton)
            ;
        StartupFacade.ConfigureViewViewModels(services);
        services.AddWindows();
        StartupFacade.ConfigureWindowViewModels(services);
        return services;
    }

    private static IServiceProvider ToScopedWhenScoped(this IServiceProvider p, ServiceLifetime lifetime = ServiceLifetime.Scoped) 
        => lifetime == ServiceLifetime.Scoped ? p.CreateScope().ServiceProvider : p;
    
    
    public static IServiceCollection AddViewAndViewModels<TView,TViewModel>(this IServiceCollection services,
        ServiceLifetime lifetime,
        Func<IServiceProvider, TView>? viewImplFactory = default,
        Func<IServiceProvider, TViewModel>? viewModelImplFactory = default,
        Action<IServiceProvider, TView>? postViewCreationAction = default, 
        Action<IServiceProvider, TViewModel>? postViewModelCreationAction = default,
        bool setDataContext = false)
        where TView : ContentControl, IViewFor<TViewModel>
        where TViewModel : class
    {
        // Register ViewModel
        services.Add(ServiceDescriptor.Describe(typeof(TViewModel), p =>
        {
            p = p.ToScopedWhenScoped(lifetime);
            var viewModel = viewModelImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance<TViewModel>(p);
            postViewModelCreationAction?.Invoke(p, viewModel);
            return viewModel;
        }, lifetime is ServiceLifetime.Scoped ? ServiceLifetime.Transient : lifetime));

        // Register View
        // Views should be Transient when not [SingleInstanceView], but Singleton or scoped can be buggy except for MainView.
        var viewLifetime = ServiceLifetime.Transient;
        
        services.Add(ServiceDescriptor.Describe(typeof(TView), p =>
        {
            var view = viewImplFactory?.Invoke(p) ?? ActivatorUtilities.CreateInstance<TView>(p);
            if (setDataContext) view.DataContext = p.GetRequiredService<TViewModel>();
            postViewCreationAction?.Invoke(p, view);
            return view;
        }, viewLifetime));
        
        services.Add(ServiceDescriptor.Describe(typeof(IReactiveViewFor<TViewModel>), 
            p => p.GetRequiredService<TView>(), viewLifetime));

        services.Add(ServiceDescriptor.Describe(typeof(IViewFor<TViewModel>), 
            p => p.GetRequiredService<TView>(), viewLifetime));
        
        ReactiveViewLocator.DictOfViews[typeof(TViewModel).FullName ?? throw new NullReferenceException()] = typeof(TView);
        
        return services;
    }

    private static IServiceCollection AddWindows(this IServiceCollection services)
        => !Globals.IsClassicDesktopStyleApplicationLifetime 
            ? services : services
                .AddViewAndViewModels<MainWindow, MainWindowViewModel>(ServiceLifetime.Singleton, setDataContext: true);
}