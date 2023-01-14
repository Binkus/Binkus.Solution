// using Binkus.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Binkus.Ioc.Tests;

public sealed class IocContainerTests
{
    private readonly IocContainerScope _containerScope;
    private readonly IServiceProvider _msDiServiceProvider;

    private const bool UseImplTypeInsteadOfFactory = true;
    private const bool UseContainerScopeWithDescriptorList = true;
    
    public IocContainerTests()
    {
        _containerScope = CreateContainerScope();
        SetupIocUtilities.SetIocUtilitiesForIocUtilitiesDelegation(_containerScope);
        // _containerScope.GetIocUtilities().FuncCreateInstance = ActivatorUtilities.CreateInstance;
        
        _msDiServiceProvider = CreateMsDiServiceProvider();
    }

    #region Setup
    
    private static IocContainerScope CreateContainerScope() => 
        UseContainerScopeWithDescriptorList
            ? CreateContainerScopeWithDescriptorList()
            : CreateContainerScopeWithCollectionInitializer();

    private static IocContainerScope CreateContainerScopeWithCollectionInitializer() =>
        UseImplTypeInsteadOfFactory
            ? new IocContainerScope
            {
                (IocDescriptor.CreateSingleton<ISingletonService, TestRecordService>()),
                (IocDescriptor.CreateScoped<IScopedService, TestRecordService>()),
                (IocDescriptor.CreateTransient<ITransientService, TestRecordService>()),
                
                (IocDescriptor.CreateSingleton<IInnerRequesterSingletonService, InnerRequesterTestRecordService>()),
                (IocDescriptor.CreateScoped<IInnerRequesterScopedService, InnerRequesterTestRecordService>()),
                (IocDescriptor.CreateTransient<IInnerRequesterTransientService, InnerRequesterTestRecordService>()),
            }
            : new IocContainerScope
            {
                (IocDescriptor.CreateSingleton<ISingletonService>(_ => new TestRecordService())),
                (IocDescriptor.CreateScoped<IScopedService>(_ => new TestRecordService())),
                (IocDescriptor.CreateTransient<ITransientService>(_ => new TestRecordService())),

                (IocDescriptor.CreateSingleton<IInnerRequesterSingletonService, InnerRequesterTestRecordService>()),
                (IocDescriptor.CreateScoped<IInnerRequesterScopedService, InnerRequesterTestRecordService>()),
                (IocDescriptor.CreateTransient<IInnerRequesterTransientService, InnerRequesterTestRecordService>()),
            };

    private static IocContainerScope CreateContainerScopeWithDescriptorList()
    {
        List<IocDescriptor> descriptors = new(6);

        if (UseImplTypeInsteadOfFactory)
        {
            descriptors.Add(IocDescriptor.CreateSingleton<ISingletonService, TestRecordService>());
            descriptors.Add(IocDescriptor.CreateScoped<IScopedService, TestRecordService>());
            descriptors.Add(IocDescriptor.CreateTransient<ITransientService, TestRecordService>());
        
            descriptors.Add(IocDescriptor.CreateSingleton<IInnerRequesterSingletonService, InnerRequesterTestRecordService>());
            descriptors.Add(IocDescriptor.CreateScoped<IInnerRequesterScopedService, InnerRequesterTestRecordService>());
            descriptors.Add(IocDescriptor.CreateTransient<IInnerRequesterTransientService, InnerRequesterTestRecordService>());    
        }
        else
        {
            descriptors.Add(IocDescriptor.CreateSingleton<ISingletonService>(_ => new TestRecordService()));
            descriptors.Add(IocDescriptor.CreateScoped<IScopedService>(_ => new TestRecordService()));
            descriptors.Add(IocDescriptor.CreateTransient<ITransientService>(_ => new TestRecordService()));
        
            descriptors.Add(IocDescriptor.CreateSingleton<IInnerRequesterSingletonService, InnerRequesterTestRecordService>());
            descriptors.Add(IocDescriptor.CreateScoped<IInnerRequesterScopedService, InnerRequesterTestRecordService>());
            descriptors.Add(IocDescriptor.CreateTransient<IInnerRequesterTransientService, InnerRequesterTestRecordService>());    
        }

        return new IocContainerScope(descriptors);
    }

    private static IServiceProvider CreateMsDiServiceProvider()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        ServiceCollection services = new();

        if (UseImplTypeInsteadOfFactory)
        {
            services.Add(ServiceDescriptor.Singleton<ISingletonService, TestRecordService>());
            services.Add(ServiceDescriptor.Scoped<IScopedService, TestRecordService>());
            services.Add(ServiceDescriptor.Transient<ITransientService, TestRecordService>());
        
            services.Add(ServiceDescriptor.Singleton<IInnerRequesterSingletonService, InnerRequesterTestRecordService>());
            services.Add(ServiceDescriptor.Scoped<IInnerRequesterScopedService, InnerRequesterTestRecordService>());
            services.Add(ServiceDescriptor.Transient<IInnerRequesterTransientService, InnerRequesterTestRecordService>());    
        }
        else
        {
            services.Add(ServiceDescriptor.Singleton<ISingletonService>(_ => new TestRecordService()));
            services.Add(ServiceDescriptor.Scoped<IScopedService>(_ => new TestRecordService()));
            services.Add(ServiceDescriptor.Transient<ITransientService>(_ => new TestRecordService()));
        
            services.Add(ServiceDescriptor.Singleton<IInnerRequesterSingletonService, InnerRequesterTestRecordService>());
            services.Add(ServiceDescriptor.Scoped<IInnerRequesterScopedService, InnerRequesterTestRecordService>());
            services.Add(ServiceDescriptor.Transient<IInnerRequesterTransientService, InnerRequesterTestRecordService>());            
        }

        return services.BuildServiceProvider();
    }
    
    #endregion

    //

    [Fact]
    public void TestBasicContainer()
    {
        var single0 = _containerScope.GetRequiredService<ISingletonService>();
        var scoped0 = _containerScope.GetRequiredService<IScopedService>();
        var transient0 = _containerScope.GetRequiredService<ITransientService>();
        
        var single1 = _containerScope.GetRequiredService<ISingletonService>();
        var scoped1 = _containerScope.GetRequiredService<IScopedService>();
        var transient1 = _containerScope.GetRequiredService<ITransientService>();

        var newScope = _containerScope.CreateScope();
        
        var newSingle0 = newScope.GetRequiredService<ISingletonService>();
        var newScoped0 = newScope.GetRequiredService<IScopedService>();
        var newTransient0 = newScope.GetRequiredService<ITransientService>();
        
        var newSingle1 = newScope.GetRequiredService<ISingletonService>();
        var newScoped1 = newScope.GetRequiredService<IScopedService>();
        var newTransient1 = newScope.GetRequiredService<ITransientService>();
        
        var new2Scope = _containerScope.CreateScope();
        
        var new2Single0 = new2Scope.GetRequiredService<ISingletonService>();
        var new2Scoped0 = new2Scope.GetRequiredService<IScopedService>();
        var new2Transient0 = new2Scope.GetRequiredService<ITransientService>();
        
        var new2Single1 = new2Scope.GetRequiredService<ISingletonService>();
        var new2Scoped1 = new2Scope.GetRequiredService<IScopedService>();
        var new2Transient1 = new2Scope.GetRequiredService<ITransientService>();
        
        
        Assert.Equal(single0, single1);
        Assert.Equal(scoped0, scoped1);
        Assert.NotEqual(transient0, transient1);
        
        Assert.Equal(newSingle0, newSingle1);
        Assert.Equal(newScoped0, newScoped1);
        Assert.NotEqual(newTransient0, newTransient1);
        
        Assert.Equal(single0, newSingle0);
        Assert.NotEqual(scoped0, newScoped0);
        Assert.NotEqual(transient0, newTransient0);
        
        //
        
        Assert.Equal(new2Single0, new2Single1);
        Assert.Equal(new2Scoped0, new2Scoped1);
        Assert.NotEqual(new2Transient0, new2Transient1);
        
        Assert.Equal(single0, new2Single0);
        Assert.NotEqual(scoped0, new2Scoped0);
        Assert.NotEqual(newScoped0, new2Scoped0);
        Assert.NotEqual(transient0, new2Transient0);
        
        Assert.Equal(newSingle0, new2Single0);
        Assert.NotEqual(newScoped0, new2Scoped0);
        Assert.NotEqual(newTransient0, new2Transient0);
    }

    [Fact]
    public void TestContainer()
    {
        var single0 = _containerScope.GetRequiredService<IInnerRequesterSingletonService>();
        var scoped0 = _containerScope.GetRequiredService<IInnerRequesterScopedService>(); // broken in root scope currently
        var transient0 = _containerScope.GetRequiredService<IInnerRequesterTransientService>();
        
        var single1 = _containerScope.GetRequiredService<IInnerRequesterSingletonService>();
        var scoped1 = _containerScope.GetRequiredService<IInnerRequesterScopedService>(); // broken in root scope currently
        var transient1 = _containerScope.GetRequiredService<IInnerRequesterTransientService>();

        var newScope = _containerScope.CreateScope();
        
        var newSingle0 = newScope.GetRequiredService<IInnerRequesterSingletonService>();
        var newScoped0 = newScope.GetRequiredService<IInnerRequesterScopedService>();
        var newTransient0 = newScope.GetRequiredService<IInnerRequesterTransientService>();
        
        var newSingle1 = newScope.GetRequiredService<IInnerRequesterSingletonService>();
        var newScoped1 = newScope.GetRequiredService<IInnerRequesterScopedService>();
        var newTransient1 = newScope.GetRequiredService<IInnerRequesterTransientService>();
        
        var new2Scope = newScope.CreateScope();
        
        var new2Single0 = new2Scope.GetRequiredService<IInnerRequesterSingletonService>();
        var new2Scoped0 = new2Scope.GetRequiredService<IInnerRequesterScopedService>();
        var new2Transient0 = new2Scope.GetRequiredService<IInnerRequesterTransientService>();
        
        var new2Single1 = new2Scope.GetRequiredService<IInnerRequesterSingletonService>();
        var new2Scoped1 = new2Scope.GetRequiredService<IInnerRequesterScopedService>();
        var new2Transient1 = new2Scope.GetRequiredService<IInnerRequesterTransientService>();
        
        Assert.Equal(single0, single1);
        Assert.Equal(scoped0, scoped1);
        Assert.NotEqual(transient0, transient1);
        
        Assert.Equal(newSingle0, newSingle1);
        Assert.Equal(newScoped0, newScoped1);
        Assert.NotEqual(newTransient0, newTransient1);
        
        Assert.Equal(single0, newSingle0);
        Assert.NotEqual(scoped0, newScoped0);
        Assert.NotEqual(transient0, newTransient0);
        
        //
        
        Assert.Equal(new2Single0, new2Single1);
        Assert.Equal(new2Scoped0, new2Scoped1);
        Assert.NotEqual(new2Transient0, new2Transient1);
        
        Assert.Equal(single0, new2Single0);
        Assert.NotEqual(scoped0, new2Scoped0);
        Assert.NotEqual(newScoped0, new2Scoped0);
        Assert.NotEqual(transient0, new2Transient0);
        
        Assert.Equal(newSingle0, new2Single0);
        Assert.NotEqual(newScoped0, new2Scoped0);
        Assert.NotEqual(newTransient0, new2Transient0);
        
        //
        
        // Assert.True(single0 == single1);
        // Assert.True(scoped0 == scoped1);
        // Assert.True(transient0 != transient1);
        //
        // Assert.True(newSingle0 == newSingle1);
        // Assert.True(newScoped0 == newScoped1);
        // Assert.True(newTransient0 != newTransient1);
        //
        // Assert.True(single0 == newSingle0);
        // Assert.True(scoped0 != newScoped0);
        // Assert.True(transient0 != newTransient0);
        //
        // //
        //
        // Assert.True(new2Single0 == new2Single1);
        // Assert.True(new2Scoped0 == new2Scoped1);
        // Assert.True(new2Transient0 != new2Transient1);
        //
        // Assert.True(single0 == new2Single0);
        // Assert.True(scoped0 != new2Scoped0);
        // Assert.True(newScoped0 != new2Scoped0);
        // Assert.True(transient0 != new2Transient0);
        //
        // Assert.True(newSingle0 == new2Single0);
        // Assert.True(newScoped0 != new2Scoped0);
        // Assert.True(newTransient0 != new2Transient0);
    }
    
    //
    
    #region MS DI
    
    //
    // MS DI
    
    [Fact]
    public void TestBasicMsDiContainer()
    {
        var single0 = _msDiServiceProvider.GetRequiredService<ISingletonService>();
        var scoped0 = _msDiServiceProvider.GetRequiredService<IScopedService>();
        var transient0 = _msDiServiceProvider.GetRequiredService<ITransientService>();
        
        var single1 = _msDiServiceProvider.GetRequiredService<ISingletonService>();
        var scoped1 = _msDiServiceProvider.GetRequiredService<IScopedService>();
        var transient1 = _msDiServiceProvider.GetRequiredService<ITransientService>();

        var newScope = _msDiServiceProvider.CreateScope().ServiceProvider;
        
        var newSingle0 = newScope.GetRequiredService<ISingletonService>();
        var newScoped0 = newScope.GetRequiredService<IScopedService>();
        var newTransient0 = newScope.GetRequiredService<ITransientService>();
        
        var newSingle1 = newScope.GetRequiredService<ISingletonService>();
        var newScoped1 = newScope.GetRequiredService<IScopedService>();
        var newTransient1 = newScope.GetRequiredService<ITransientService>();
        
        var new2Scope = _msDiServiceProvider.CreateScope().ServiceProvider;
        
        var new2Single0 = new2Scope.GetRequiredService<ISingletonService>();
        var new2Scoped0 = new2Scope.GetRequiredService<IScopedService>();
        var new2Transient0 = new2Scope.GetRequiredService<ITransientService>();
        
        var new2Single1 = new2Scope.GetRequiredService<ISingletonService>();
        var new2Scoped1 = new2Scope.GetRequiredService<IScopedService>();
        var new2Transient1 = new2Scope.GetRequiredService<ITransientService>();
        
        
        Assert.Equal(single0, single1);
        Assert.Equal(scoped0, scoped1);
        Assert.NotEqual(transient0, transient1);
        
        Assert.Equal(newSingle0, newSingle1);
        Assert.Equal(newScoped0, newScoped1);
        Assert.NotEqual(newTransient0, newTransient1);
        
        Assert.Equal(single0, newSingle0);
        Assert.NotEqual(scoped0, newScoped0);
        Assert.NotEqual(transient0, newTransient0);
        
        //
        
        Assert.Equal(new2Single0, new2Single1);
        Assert.Equal(new2Scoped0, new2Scoped1);
        Assert.NotEqual(new2Transient0, new2Transient1);
        
        Assert.Equal(single0, new2Single0);
        Assert.NotEqual(scoped0, new2Scoped0);
        Assert.NotEqual(newScoped0, new2Scoped0);
        Assert.NotEqual(transient0, new2Transient0);
        
        Assert.Equal(newSingle0, new2Single0);
        Assert.NotEqual(newScoped0, new2Scoped0);
        Assert.NotEqual(newTransient0, new2Transient0);
    }
    
    //
    
    [Fact]
    public void TestMsDiContainer()
    {
        var single0 = _msDiServiceProvider.GetRequiredService<IInnerRequesterSingletonService>();
        var scoped0 = _msDiServiceProvider.GetRequiredService<IInnerRequesterScopedService>();
        var transient0 = _msDiServiceProvider.GetRequiredService<IInnerRequesterTransientService>();
        
        var single1 = _msDiServiceProvider.GetRequiredService<IInnerRequesterSingletonService>();
        var scoped1 = _msDiServiceProvider.GetRequiredService<IInnerRequesterScopedService>();
        var transient1 = _msDiServiceProvider.GetRequiredService<IInnerRequesterTransientService>();

        var newScope = _msDiServiceProvider.CreateScope().ServiceProvider;
        
        var newSingle0 = newScope.GetRequiredService<IInnerRequesterSingletonService>();
        var newScoped0 = newScope.GetRequiredService<IInnerRequesterScopedService>();
        var newTransient0 = newScope.GetRequiredService<IInnerRequesterTransientService>();
        
        var newSingle1 = newScope.GetRequiredService<IInnerRequesterSingletonService>();
        var newScoped1 = newScope.GetRequiredService<IInnerRequesterScopedService>();
        var newTransient1 = newScope.GetRequiredService<IInnerRequesterTransientService>();
        
        var new2Scope = _msDiServiceProvider.CreateScope().ServiceProvider;
        
        var new2Single0 = new2Scope.GetRequiredService<IInnerRequesterSingletonService>();
        var new2Scoped0 = new2Scope.GetRequiredService<IInnerRequesterScopedService>();
        var new2Transient0 = new2Scope.GetRequiredService<IInnerRequesterTransientService>();
        
        var new2Single1 = new2Scope.GetRequiredService<IInnerRequesterSingletonService>();
        var new2Scoped1 = new2Scope.GetRequiredService<IInnerRequesterScopedService>();
        var new2Transient1 = new2Scope.GetRequiredService<IInnerRequesterTransientService>();
        
        Assert.Equal(single0, single1);
        Assert.Equal(scoped0, scoped1);
        Assert.NotEqual(transient0, transient1);
        
        Assert.Equal(newSingle0, newSingle1);
        Assert.Equal(newScoped0, newScoped1);
        Assert.NotEqual(newTransient0, newTransient1);
        
        Assert.Equal(single0, newSingle0);
        Assert.NotEqual(scoped0, newScoped0);
        Assert.NotEqual(transient0, newTransient0);
        
        //
        
        Assert.Equal(new2Single0, new2Single1);
        Assert.Equal(new2Scoped0, new2Scoped1);
        Assert.NotEqual(new2Transient0, new2Transient1);
        
        Assert.Equal(single0, new2Single0);
        Assert.NotEqual(scoped0, new2Scoped0);
        Assert.NotEqual(newScoped0, new2Scoped0);
        Assert.NotEqual(transient0, new2Transient0);
        
        Assert.Equal(newSingle0, new2Single0);
        Assert.NotEqual(newScoped0, new2Scoped0);
        Assert.NotEqual(newTransient0, new2Transient0);
    }
    
    #endregion
}