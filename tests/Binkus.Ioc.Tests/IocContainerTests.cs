// using Binkus.DependencyInjection.Extensions;

using System.Collections;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Binkus.Ioc.Tests;

public sealed class IocContainerTests
{
    private readonly IocContainerScope _containerScope;
    private readonly IServiceProvider _msDiServiceProvider;

    private const bool UseImplTypeInsteadOfFactory = true;
    private const bool UseContainerScopeWithDescriptorList = true;
    private const bool UseIocUtilitiesInsteadOfActivatorUtilities = true;
    
    public IocContainerTests()
    {
        _msDiServiceProvider = CreateMsDiServiceProvider();
        _containerScope = CreateContainerScope();
        SetIocUtilities(_containerScope);
        SetIocUtilities(_msDiServiceProvider);
    }

    #region Setup

    private static void SetIocUtilities(IServiceProvider iocContainerScope)
    {
        if (UseIocUtilitiesInsteadOfActivatorUtilities)
        {
            SetupIocUtilities.SetIocUtilitiesForIocUtilitiesDelegation(iocContainerScope);
            return;
        }
        iocContainerScope.GetIocUtilities().FuncCreateInstance = ActivatorUtilities.CreateInstance;
    }
    
    private static IocContainerScope CreateContainerScope() => 
        UseContainerScopeWithDescriptorList
            ? CreateContainerScopeWithDescriptorList()
            : CreateContainerScopeWithCollectionInitializer();

    private static IocContainerScope AddSpecials(IocContainerScope services)
    {
        // AddLazyResolution(services);
        return services;
    }

    private static void AddLazyResolution(IocContainerScope services)
    {
        var lazyResolution = IocDescriptor.CreateTransient(typeof(Lazy<>), typeof(LazilyResolved<>));
        services.Add(lazyResolution);
    }

    private static IocContainerScope CreateContainerScopeWithCollectionInitializer() =>
        AddSpecials(UseImplTypeInsteadOfFactory
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
            });

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

        return AddSpecials(new IocContainerScope(descriptors));
    }

    private static IServiceProvider CreateMsDiServiceProvider()
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        ServiceCollection services = new();

        AddLazyResolution(services);

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
    
    private static IServiceCollection AddLazyResolution(IServiceCollection services) 
        => services.AddTransient(
            typeof(Lazy<>),
            typeof(LazilyResolved<>));

    private sealed class LazilyResolved<T> : Lazy<T> where T : notnull
    {
        public LazilyResolved(IServiceProvider serviceProvider)
            : base(serviceProvider.GetIocUtilities().GetServiceOrCreateInstance<T>(serviceProvider))
        {
        }
        // public LazilyResolved(IServiceProvider serviceProvider)
        //     : base(ActivatorUtilities.GetServiceOrCreateInstance<T>(serviceProvider))
        // {
        // }
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
        var scoped0 = _containerScope.GetRequiredService<IInnerRequesterScopedService>();
        var transient0 = _containerScope.GetRequiredService<IInnerRequesterTransientService>();
        
        var single1 = _containerScope.GetRequiredService<IInnerRequesterSingletonService>();
        var scoped1 = _containerScope.GetRequiredService<IInnerRequesterScopedService>();
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
    }

    public static readonly IEnumerable<object[]> AllLifetimesTestData =
        new object[][]
        {
            // new object[] { IocLifetime.Singleton, false, },
            // new object[] { IocLifetime.Scoped, false, },
            // new object[] { IocLifetime.Transient, false, },
            //
            // new object[] { IocLifetime.Singleton, true, },
            // new object[] { IocLifetime.Scoped, true, },
            // new object[] { IocLifetime.Transient, true, },
            
            new object[] { IocLifetime.Singleton, },
            new object[] { IocLifetime.Scoped, },
            new object[] { IocLifetime.Transient, },
        };

    [Theory]
    [MemberData(nameof(AllLifetimesTestData))]
    public void IocContainerScopeWithOpenGenericServices_ShouldResolveWithCorrectLifetimes(IocLifetime lifetime)
    {
        var d = IocDescriptor.Create(lifetime, typeof(IGenericService<>),
            typeof(TestGenericRecordService<>));
        d.ThrowOnInvalidity();
        _containerScope.Add(d);
        Assert.Contains(d, _containerScope.Root.Descriptors);
        Assert.Contains(d, _containerScope.Root.CachedDescriptors.Values);
        
        // var genericServiceOfObject = _containerScope.GetService<IGenericService<object>>();
        // var genericServiceOfInt = _containerScope.GetService<IGenericService<int>>();
        // var genericServiceOfIocDescriptor = _containerScope.GetService<IGenericService<IocDescriptor>>();
        //
        // var genericServiceOfObject2 = _containerScope.GetService<IGenericService<object>>();
        // var genericServiceOfInt2 = _containerScope.GetService<IGenericService<int>>();
        // var genericServiceOfIocDescriptor2 = _containerScope.GetService<IGenericService<IocDescriptor>>();
        //
        // var scope = _containerScope.CreateScope();
        // Assert.NotNull(scope);
        //
        // var genericServiceOfObjectFromNewScope = scope.GetService<IGenericService<object>>();
        // var genericServiceOfIntFromNewScope = scope.GetService<IGenericService<int>>();
        // var genericServiceOfIocDescriptorFromNewScope = scope.GetService<IGenericService<IocDescriptor>>();
        
        //
        
        var genericServiceOfObject = _containerScope.GetService(typeof(IGenericService<object>));
        var genericServiceOfInt = _containerScope.GetService(typeof(IGenericService<int>));
        var genericServiceOfIocDescriptor = _containerScope.GetService(typeof(IGenericService<IocDescriptor>));

        var genericServiceOfObject2 = _containerScope.GetService(typeof(IGenericService<object>));
        var genericServiceOfInt2 = _containerScope.GetService(typeof(IGenericService<int>));
        var genericServiceOfIocDescriptor2 = _containerScope.GetService(typeof(IGenericService<IocDescriptor>));
        
        var scope = _containerScope.CreateScope();
        Assert.NotNull(scope);
        
        var genericServiceOfObjectFromNewScope = scope.GetService(typeof(IGenericService<object>));
        var genericServiceOfIntFromNewScope = scope.GetService(typeof(IGenericService<int>));
        var genericServiceOfIocDescriptorFromNewScope = scope.GetService(typeof(IGenericService<IocDescriptor>));
        
        //

        Assert.NotNull(genericServiceOfObject);
        Assert.NotNull(genericServiceOfInt);
        Assert.NotNull(genericServiceOfIocDescriptor);
        
        Assert.NotNull(genericServiceOfObject2);
        Assert.NotNull(genericServiceOfInt2);
        Assert.NotNull(genericServiceOfIocDescriptor2);
        
        Assert.NotNull(genericServiceOfObjectFromNewScope);
        Assert.NotNull(genericServiceOfIntFromNewScope);
        Assert.NotNull(genericServiceOfIocDescriptorFromNewScope);
        
        Assert.IsAssignableFrom<IGenericService<object>>(genericServiceOfObject);
        Assert.IsAssignableFrom<IGenericService<int>>(genericServiceOfInt);
        Assert.IsAssignableFrom<IGenericService<IocDescriptor>>(genericServiceOfIocDescriptor);
        
        Assert.IsAssignableFrom<IGenericService<object>>(genericServiceOfObject2);
        Assert.IsAssignableFrom<IGenericService<int>>(genericServiceOfInt2);
        Assert.IsAssignableFrom<IGenericService<IocDescriptor>>(genericServiceOfIocDescriptor2);
        
        Assert.IsAssignableFrom<IGenericService<object>>(genericServiceOfObjectFromNewScope);
        Assert.IsAssignableFrom<IGenericService<int>>(genericServiceOfIntFromNewScope);
        Assert.IsAssignableFrom<IGenericService<IocDescriptor>>(genericServiceOfIocDescriptorFromNewScope);

        switch (lifetime)
        {
            case IocLifetime.Singleton:
                AssertSingleton();
                break;
            case IocLifetime.Scoped:
                AssertScoped();
                break;
            case IocLifetime.Transient:
                AssertTransient();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
        }
        
        void AssertSingleton()
        {
            SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfObject, genericServiceOfObject2);
            SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfObject, genericServiceOfObjectFromNewScope);
            SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfInt, genericServiceOfInt2);
            SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfInt, genericServiceOfIntFromNewScope);
            SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfIocDescriptor, genericServiceOfIocDescriptor2);
            SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfIocDescriptor, genericServiceOfIocDescriptorFromNewScope);
        }

        void AssertScoped()
        {
            ScopedServices_ResolvedFromSameScope_ShouldBeEqual(genericServiceOfObject, genericServiceOfObject2);
            ScopedServices_ResolvedFromDifferentScopes_ShouldBeNotEqual(genericServiceOfObject, genericServiceOfObjectFromNewScope);
            ScopedServices_ResolvedFromSameScope_ShouldBeEqual(genericServiceOfInt, genericServiceOfInt2);
            ScopedServices_ResolvedFromDifferentScopes_ShouldBeNotEqual(genericServiceOfInt, genericServiceOfIntFromNewScope);
            ScopedServices_ResolvedFromSameScope_ShouldBeEqual(genericServiceOfIocDescriptor, genericServiceOfIocDescriptor2);
            ScopedServices_ResolvedFromDifferentScopes_ShouldBeNotEqual(genericServiceOfIocDescriptor, genericServiceOfIocDescriptorFromNewScope);
        }
        
        void AssertTransient()
        {
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfObject, genericServiceOfObject2);
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfObject, genericServiceOfObjectFromNewScope);
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfObject2, genericServiceOfObjectFromNewScope);
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfInt, genericServiceOfInt2);
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfInt, genericServiceOfIntFromNewScope);
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfInt2, genericServiceOfIntFromNewScope);
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfIocDescriptor, genericServiceOfIocDescriptor2);
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfIocDescriptor, genericServiceOfIocDescriptorFromNewScope);
            TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(genericServiceOfIocDescriptor2, genericServiceOfIocDescriptorFromNewScope);
        }
    }
    
    // [Fact]
    // public void TestContainerSpecials_OpenGenericFactoryDescriptor_ShouldInstantiate()
    // {
    //     var d = IocDescriptor.Create(IocLifetime.Singleton, typeof(IGenericService<>),
    //         typeof(TestGenericRecordService<>));
    //     _containerScope.Add(d);
    //     var genericServiceOfObject = _containerScope.GetService<IGenericService<object>>();
    //     var genericServiceOfInt = _containerScope.GetService<IGenericService<int>>();
    //     var genericServiceOfIocDescriptor = _containerScope.GetService<IGenericService<IocDescriptor>>();
    //     
    //     var genericServiceOfObject2 = _containerScope.GetService<IGenericService<object>>();
    //     var genericServiceOfInt2 = _containerScope.GetService<IGenericService<int>>();
    //     var genericServiceOfIocDescriptor2 = _containerScope.GetService<IGenericService<IocDescriptor>>();
    //     
    //     Assert.NotNull(genericServiceOfObject);
    //     Assert.NotNull(genericServiceOfInt);
    //     Assert.NotNull(genericServiceOfIocDescriptor);
    //     Assert.NotNull(genericServiceOfObject2);
    //     Assert.NotNull(genericServiceOfInt2);
    //     Assert.NotNull(genericServiceOfIocDescriptor2);
    //     SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfObject, genericServiceOfObject2);
    //     SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfInt, genericServiceOfInt2);
    //     SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(genericServiceOfIocDescriptor, genericServiceOfIocDescriptor2);
    // }

    private static void TransientServices_ResolvedFromAnyScope_ShouldBeNotEqual(object? expected, object? actual) =>
        Assert.NotEqual(expected, actual);
    private static void ScopedServices_ResolvedFromDifferentScopes_ShouldBeNotEqual(object? expected, object? actual) =>
        Assert.NotEqual(expected, actual);
    private static void ScopedServices_ResolvedFromSameScope_ShouldBeEqual(object? expected, object? actual) =>
        Assert.Equal(expected, actual);
    private static void SingletonServices_ResolvedFromAnyScope_ShouldBeEqual(object? expected, object? actual) =>
        Assert.Equal(expected, actual);
    // private static void __Assert_SingletonsFromEverywhereOrScopedFromSameScope_Are_Equal(object? left, object? right) =>
    //     Assert.Equal(left, right);


    [Fact]
    public void TestContainerSpecialsEnumerableOfLazyT()
    {
        Assert.NotNull(_containerScope);
        var single00 = _containerScope.GetRequiredService<IEnumerable<IInnerRequesterSingletonService>>().GetEnumerator().TryGetNext();
        
        var single0 = _containerScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterSingletonService>>>().GetEnumerator().TryGetNext();
        var scoped0 = _containerScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterScopedService>>>().GetEnumerator().TryGetNext();
        var transient0 = _containerScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterTransientService>>>().GetEnumerator().TryGetNext();
        
        var single1 = _containerScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterSingletonService>>>().GetEnumerator().TryGetNext();
        var scoped1 = _containerScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterScopedService>>>().GetEnumerator().TryGetNext();
        var transient1 = _containerScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterTransientService>>>().GetEnumerator().TryGetNext();

        var newScope = _containerScope.CreateScope();
        Assert.NotNull(newScope);
        
        var newSingle0 = newScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterSingletonService>>>().GetEnumerator().TryGetNext();
        var newScoped0 = newScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterScopedService>>>().GetEnumerator().TryGetNext();
        var newTransient0 = newScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterTransientService>>>().GetEnumerator().TryGetNext();
        
        var newSingle1 = newScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterSingletonService>>>().GetEnumerator().TryGetNext();
        var newScoped1 = newScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterScopedService>>>().GetEnumerator().TryGetNext();
        var newTransient1 = newScope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterTransientService>>>().GetEnumerator().TryGetNext();
        
        var new2Scope = newScope.CreateScope();
        Assert.NotNull(new2Scope);
        
        var new2Single0 = new2Scope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterSingletonService>>>().GetEnumerator().TryGetNext();
        var new2Scoped0 = new2Scope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterScopedService>>>().GetEnumerator().TryGetNext();
        var new2Transient0 = new2Scope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterTransientService>>>().GetEnumerator().TryGetNext();
        
        var new2Single1 = new2Scope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterSingletonService>>>().GetEnumerator().TryGetNext();
        var new2Scoped1 = new2Scope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterScopedService>>>().GetEnumerator().TryGetNext();
        var new2Transient1 = new2Scope.GetRequiredService<IEnumerable<Lazy<IInnerRequesterTransientService>>>().GetEnumerator().TryGetNext();
        
        Assert.NotNull(single00);
        
        Assert.NotNull(single0);
        Assert.NotNull(scoped0);
        Assert.NotNull(transient0);
        
        Assert.NotNull(single1);
        Assert.NotNull(scoped1);
        Assert.NotNull(transient1);
        
        Assert.NotNull(newSingle0);
        Assert.NotNull(newScoped0);
        Assert.NotNull(newTransient0);
        
        Assert.NotNull(newSingle1);
        Assert.NotNull(newScoped1);
        Assert.NotNull(newTransient1);
        
        Assert.NotNull(new2Single0);
        Assert.NotNull(new2Scoped0);
        Assert.NotNull(new2Transient0);
        
        Assert.NotNull(new2Single1);
        Assert.NotNull(new2Scoped1);
        Assert.NotNull(new2Transient1);
        
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
    public void TestContainerSpecialsLazyT()
    {
        var single0 = _containerScope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var scoped0 = _containerScope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var transient0 = _containerScope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
        var single1 = _containerScope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var scoped1 = _containerScope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var transient1 = _containerScope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;

        var newScope = _containerScope.CreateScope();
        
        var newSingle0 = newScope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var newScoped0 = newScope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var newTransient0 = newScope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
        var newSingle1 = newScope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var newScoped1 = newScope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var newTransient1 = newScope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
        var new2Scope = newScope.CreateScope();
        
        var new2Single0 = new2Scope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var new2Scoped0 = new2Scope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var new2Transient0 = new2Scope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
        var new2Single1 = new2Scope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var new2Scoped1 = new2Scope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var new2Transient1 = new2Scope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
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
    
    [Fact]
    public void TestMsDiContainerSpecialsLazyT()
    {
        var single0 = _msDiServiceProvider.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var scoped0 = _msDiServiceProvider.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var transient0 = _msDiServiceProvider.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
        var single1 = _msDiServiceProvider.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var scoped1 = _msDiServiceProvider.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var transient1 = _msDiServiceProvider.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;

        var newScope = _msDiServiceProvider.CreateScope().ServiceProvider;
        
        var newSingle0 = newScope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var newScoped0 = newScope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var newTransient0 = newScope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
        var newSingle1 = newScope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var newScoped1 = newScope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var newTransient1 = newScope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
        var new2Scope = _msDiServiceProvider.CreateScope().ServiceProvider;
        
        var new2Single0 = new2Scope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var new2Scoped0 = new2Scope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var new2Transient0 = new2Scope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
        var new2Single1 = new2Scope.GetRequiredService<Lazy<IInnerRequesterSingletonService>>().Value;
        var new2Scoped1 = new2Scope.GetRequiredService<Lazy<IInnerRequesterScopedService>>().Value;
        var new2Transient1 = new2Scope.GetRequiredService<Lazy<IInnerRequesterTransientService>>().Value;
        
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

internal static class Ext
{
    public static object? TryGetNext(this IEnumerator enumerator) => enumerator.MoveNext() ? enumerator.Current : null;
    public static T? TryGetNext<T>(this IEnumerator<T> enumerator) => enumerator.MoveNext() ? enumerator.Current : default;
}